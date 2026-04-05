using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using GoMicFuckYourself.Contracts.Audio;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace GoMicFuckYourself.Agent.Audio;

public sealed class WindowsAudioController : IAudioController, IMMNotificationClient
{
    private static readonly DeviceState ObservableDeviceStates =
        DeviceState.Active;

    private readonly MMDeviceEnumerator _enumerator;
    private readonly ILogger<WindowsAudioController> _logger;

    private readonly ConcurrentDictionary<string, string> _deviceNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MMDevice> _observedDevices = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _sync = new();

    private readonly Dictionary<string, AudioEndpointVolumeNotificationDelegate> _volumeHandlers =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _disposed;
    private int _refreshQueued;

    public WindowsAudioController(ILogger<WindowsAudioController> logger)
    {
        _logger = logger;
        _enumerator = new MMDeviceEnumerator();
        _enumerator.RegisterEndpointNotificationCallback(this);
        RefreshVolumeSubscriptions();
    }

    public event EventHandler? CaptureDevicesChanged;
    public event EventHandler<string>? DefaultCaptureDeviceChanged;
    public event EventHandler<string>? DefaultCommunicationsDeviceChanged;
    public event EventHandler<CaptureDeviceStateChangedEventArgs>? CaptureDeviceStateChanged;
    public event EventHandler<CaptureDeviceVolumeChangedEventArgs>? CaptureDeviceVolumeChanged;

    public IReadOnlyList<CaptureDeviceInfo> GetCaptureDevices()
    {
        ThrowIfDisposed();

        using var enumerator = CreateEnumerator();
        return EnumerateCaptureDevices(enumerator, false);
    }

    public string? GetDefaultCaptureDeviceId(AudioPolicyRole role)
    {
        ThrowIfDisposed();

        using var enumerator = CreateEnumerator();
        return TryGetDefaultCaptureDevice(enumerator, role)?.ID;
    }

    public CaptureDeviceInfo? GetCaptureDevice(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ThrowIfDisposed();

        using var enumerator = CreateEnumerator();
        using var device = TryGetDevice(enumerator, deviceId);
        return device is null ? null : CreateCaptureDeviceInfo(enumerator, device, cacheDeviceName: false);
    }

    public void SetCaptureVolume(string deviceId, float volumePercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ThrowIfDisposed();

        var normalizedVolume = Math.Clamp(volumePercent, 0f, 100f) / 100f;

        using var enumerator = CreateEnumerator();
        using var device = TryGetDevice(enumerator, deviceId) ??
                           throw new InvalidOperationException($"Capture device '{deviceId}' was not found.");
        device.AudioEndpointVolume.MasterVolumeLevelScalar = normalizedVolume;
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_sync)
        {
            if (_disposed) return;

            _enumerator.UnregisterEndpointNotificationCallback(this);

            foreach (var pair in _observedDevices)
            {
                TryUnsubscribeVolumeNotification(pair.Value, _volumeHandlers[pair.Key]);
                pair.Value.Dispose();
            }

            _volumeHandlers.Clear();
            _observedDevices.Clear();
            _enumerator.Dispose();
            _disposed = true;
        }
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || _disposed) return;

        var friendlyName = GetCachedDeviceName(deviceId);

        _logger.LogInformation(
            "Audio endpoint state changed. Id: {DeviceId}, Name: {DeviceName}, State: {State}.",
            deviceId,
            friendlyName ?? "<unknown>",
            MapDeviceState(newState));
        QueueRefreshVolumeSubscriptions();
        CaptureDeviceStateChanged?.Invoke(this,
            new CaptureDeviceStateChangedEventArgs(deviceId, MapDeviceState(newState)));
        CaptureDevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OnDeviceAdded(string deviceId)
    {
        if (_disposed) return;

        var friendlyName = TryResolveFriendlyNameWithoutLock(deviceId) ?? GetCachedDeviceName(deviceId);

        _logger.LogInformation(
            "Audio endpoint added. Id: {DeviceId}, Name: {DeviceName}.",
            deviceId,
            friendlyName ?? "<unknown>");
        QueueRefreshVolumeSubscriptions();
        CaptureDevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OnDeviceRemoved(string deviceId)
    {
        if (_disposed) return;

        var friendlyName = GetCachedDeviceName(deviceId);

        _logger.LogInformation(
            "Audio endpoint removed. Id: {DeviceId}, Name: {DeviceName}.",
            deviceId,
            friendlyName ?? "<unknown>");
        QueueRefreshVolumeSubscriptions();
        CaptureDevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (_disposed || flow != DataFlow.Capture) return;

        var friendlyName = TryResolveFriendlyNameWithoutLock(defaultDeviceId) ?? GetCachedDeviceName(defaultDeviceId);

        _logger.LogInformation(
            "Default capture device changed. Role: {Role}, Id: {DeviceId}, Name: {DeviceName}.",
            role,
            defaultDeviceId,
            friendlyName ?? "<unknown>");

        switch (role)
        {
            case Role.Communications:
                DefaultCommunicationsDeviceChanged?.Invoke(this, defaultDeviceId);
                break;
            case Role.Console:
            case Role.Multimedia:
                DefaultCaptureDeviceChanged?.Invoke(this, defaultDeviceId);
                break;
        }

        CaptureDevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        if (_disposed) return;

        QueueRefreshVolumeSubscriptions();
        CaptureDevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    private IReadOnlyList<CaptureDeviceInfo> EnumerateCaptureDevices(MMDeviceEnumerator enumerator, bool cacheDeviceNames)
    {
        var collection = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, ObservableDeviceStates);

        var defaultDeviceId = TryGetDefaultCaptureDevice(enumerator, AudioPolicyRole.Multimedia)?.ID;
        var defaultCommunicationsId = TryGetDefaultCaptureDevice(enumerator, AudioPolicyRole.Communications)?.ID;
        var devices = new List<CaptureDeviceInfo>(collection.Count);

        for (var index = 0; index < collection.Count; index++)
        {
            using var device = collection[index];
            try
            {
                devices.Add(CreateCaptureDeviceInfo(
                    enumerator,
                    device,
                    defaultDeviceId,
                    defaultCommunicationsId,
                    cacheDeviceNames));
            }
            catch (COMException exception)
            {
                _logger.LogDebug(exception,
                    "Falling back to limited capture device info for {DeviceId} after a COM failure during enumeration.",
                    device.ID);
                devices.Add(CreateFallbackCaptureDeviceInfo(device, defaultDeviceId, defaultCommunicationsId));
            }
            catch (ObjectDisposedException exception)
            {
                _logger.LogDebug(exception,
                    "Falling back to limited capture device info for {DeviceId} because the device was disposed during enumeration.",
                    device.ID);
                devices.Add(CreateFallbackCaptureDeviceInfo(device, defaultDeviceId, defaultCommunicationsId));
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogDebug(exception,
                    "Falling back to limited capture device info for {DeviceId} after an invalid device state during enumeration.",
                    device.ID);
                devices.Add(CreateFallbackCaptureDeviceInfo(device, defaultDeviceId, defaultCommunicationsId));
            }
        }

        return devices;
    }

    private CaptureDeviceInfo CreateCaptureDeviceInfo(
        MMDeviceEnumerator enumerator,
        MMDevice device,
        string? defaultDeviceId = null,
        string? defaultCommunicationsId = null,
        bool cacheDeviceName = true)
    {
        defaultDeviceId ??= TryGetDefaultCaptureDevice(enumerator, AudioPolicyRole.Multimedia)?.ID;
        defaultCommunicationsId ??= TryGetDefaultCaptureDevice(enumerator, AudioPolicyRole.Communications)?.ID;
        var (volumePercent, isMuted) = TryReadVolume(device);
        var friendlyName = TryGetFriendlyName(device);
        if (cacheDeviceName) CacheDeviceName(device.ID, friendlyName);

        return new CaptureDeviceInfo(
            device.ID,
            friendlyName,
            MapDeviceState(device.State),
            string.Equals(device.ID, defaultDeviceId, StringComparison.OrdinalIgnoreCase),
            string.Equals(device.ID, defaultCommunicationsId, StringComparison.OrdinalIgnoreCase),
            volumePercent,
            isMuted);
    }

    private CaptureDeviceInfo CreateFallbackCaptureDeviceInfo(MMDevice device, string? defaultDeviceId,
        string? defaultCommunicationsId)
    {
        var friendlyName = TryGetFriendlyName(device);
        CacheDeviceName(device.ID, friendlyName);

        return new CaptureDeviceInfo(
            device.ID,
            friendlyName,
            TryGetDeviceAvailability(device),
            string.Equals(device.ID, defaultDeviceId, StringComparison.OrdinalIgnoreCase),
            string.Equals(device.ID, defaultCommunicationsId, StringComparison.OrdinalIgnoreCase),
            0f,
            false);
    }

    private MMDevice? TryGetDefaultCaptureDevice(MMDeviceEnumerator enumerator, AudioPolicyRole role)
    {
        try
        {
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, MapRole(role));
        }
        catch (COMException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private MMDevice? TryGetDevice(MMDeviceEnumerator enumerator, string deviceId)
    {
        try
        {
            return enumerator.GetDevice(deviceId);
        }
        catch (COMException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private void RefreshVolumeSubscriptions()
    {
        var activeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var enumerator = CreateEnumerator();
        var collection = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, ObservableDeviceStates);

        for (var index = 0; index < collection.Count; index++)
        {
            using var device = collection[index];
            activeIds.Add(device.ID);
            CacheDeviceName(device.ID, TryGetFriendlyName(device));

            if (_observedDevices.ContainsKey(device.ID)) continue;

            TryAddObservedDevice(enumerator, device.ID);
        }

        var removedIds = _observedDevices.Keys.Where(id => !activeIds.Contains(id)).ToArray();
        foreach (var removedId in removedIds)
        {
            var observedDevice = _observedDevices[removedId];
            TryUnsubscribeVolumeNotification(observedDevice, _volumeHandlers[removedId]);
            observedDevice.Dispose();

            _observedDevices.Remove(removedId);
            _volumeHandlers.Remove(removedId);
        }
    }

    private void QueueRefreshVolumeSubscriptions()
    {
        if (_disposed) return;
        if (Interlocked.Exchange(ref _refreshQueued, 1) == 1) return;

        _ = Task.Run(() =>
        {
            try
            {
                lock (_sync)
                {
                    if (_disposed) return;

                    RefreshVolumeSubscriptions();
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to refresh capture-device volume subscriptions after an audio notification.");
            }
            finally
            {
                Interlocked.Exchange(ref _refreshQueued, 0);
            }
        });
    }

    private void TryAddObservedDevice(MMDeviceEnumerator enumerator, string deviceId)
    {
        try
        {
            var observedDevice = enumerator.GetDevice(deviceId);
            CacheDeviceName(observedDevice.ID, TryGetFriendlyName(observedDevice));
            var endpointVolume = observedDevice.AudioEndpointVolume;
            AudioEndpointVolumeNotificationDelegate handler = data => OnVolumeNotification(observedDevice.ID, data);
            endpointVolume.OnVolumeNotification += handler;

            _observedDevices.Add(observedDevice.ID, observedDevice);
            _volumeHandlers.Add(observedDevice.ID, handler);
        }
        catch (COMException exception)
        {
            _logger.LogDebug(exception,
                "Skipping volume subscription for capture device {DeviceId} because endpoint volume is unavailable.",
                deviceId);
        }
        catch (ObjectDisposedException exception)
        {
            _logger.LogDebug(exception,
                "Skipping volume subscription for capture device {DeviceId} because the device was disposed during observation.",
                deviceId);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogDebug(exception,
                "Skipping volume subscription for capture device {DeviceId} because the device could not be observed.",
                deviceId);
        }
    }

    private static (float VolumePercent, bool IsMuted) TryReadVolume(MMDevice device)
    {
        try
        {
            return (device.AudioEndpointVolume.MasterVolumeLevelScalar * 100f, device.AudioEndpointVolume.Mute);
        }
        catch (COMException)
        {
            return (0f, false);
        }
        catch (InvalidOperationException)
        {
            return (0f, false);
        }
    }

    private static string TryGetFriendlyName(MMDevice device)
    {
        try
        {
            return string.IsNullOrWhiteSpace(device.FriendlyName)
                ? $"Capture device {device.ID}"
                : device.FriendlyName;
        }
        catch (COMException)
        {
            return $"Capture device {device.ID}";
        }
        catch (InvalidOperationException)
        {
            return $"Capture device {device.ID}";
        }
    }

    private static DeviceAvailability TryGetDeviceAvailability(MMDevice device)
    {
        try
        {
            return MapDeviceState(device.State);
        }
        catch (COMException)
        {
            return DeviceAvailability.Unknown;
        }
        catch (InvalidOperationException)
        {
            return DeviceAvailability.Unknown;
        }
    }

    private static void TryUnsubscribeVolumeNotification(MMDevice device,
        AudioEndpointVolumeNotificationDelegate handler)
    {
        try
        {
            device.AudioEndpointVolume.OnVolumeNotification -= handler;
        }
        catch (COMException)
        {
            // Best-effort cleanup; endpoint may already be unavailable.
        }
        catch (ObjectDisposedException)
        {
            // Best-effort cleanup; device may already be disposed.
        }
    }

    private void OnVolumeNotification(string deviceId, AudioVolumeNotificationData data)
    {
        if (_disposed) return;

        CaptureDeviceVolumeChanged?.Invoke(
            this,
            new CaptureDeviceVolumeChangedEventArgs(deviceId, data.MasterVolume * 100f, data.Muted));
    }

    private void CacheDeviceName(string deviceId, string friendlyName)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(friendlyName)) return;

        _deviceNames[deviceId] = friendlyName;
    }

    private string? GetCachedDeviceName(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return null;

        return _deviceNames.TryGetValue(deviceId, out var friendlyName) ? friendlyName : null;
    }

    private string? TryResolveFriendlyNameWithoutLock(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return null;

        using var enumerator = CreateEnumerator();
        using var device = TryGetDevice(enumerator, deviceId);
        if (device is null) return null;

        var friendlyName = TryGetFriendlyName(device);
        CacheDeviceName(deviceId, friendlyName);
        return friendlyName;
    }

    private static MMDeviceEnumerator CreateEnumerator()
    {
        return new MMDeviceEnumerator();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static Role MapRole(AudioPolicyRole role)
    {
        return role switch
        {
            AudioPolicyRole.Console => Role.Console,
            AudioPolicyRole.Multimedia => Role.Multimedia,
            AudioPolicyRole.Communications => Role.Communications,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported audio role.")
        };
    }

    private static DeviceAvailability MapDeviceState(DeviceState state)
    {
        return state switch
        {
            DeviceState.Active => DeviceAvailability.Active,
            DeviceState.Disabled => DeviceAvailability.Disabled,
            DeviceState.NotPresent => DeviceAvailability.NotPresent,
            DeviceState.Unplugged => DeviceAvailability.Unplugged,
            _ => DeviceAvailability.Unknown
        };
    }
}
