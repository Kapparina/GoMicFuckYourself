using GoMicFuckYourself.Contracts.Audio;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace GoMicFuckYourself.Agent.Audio;

public sealed class WindowsAudioController : IAudioController, IMMNotificationClient
{
    private static readonly DeviceState AllDeviceStates =
        DeviceState.Active | DeviceState.Disabled | DeviceState.NotPresent | DeviceState.Unplugged;
    private static readonly DeviceState ObservableDeviceStates =
        DeviceState.Active;

    private readonly MMDeviceEnumerator _enumerator;
    private readonly Lock _sync = new();
    private readonly Dictionary<string, AudioEndpointVolumeNotificationDelegate> _volumeHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MMDevice> _observedDevices = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public WindowsAudioController()
    {
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

        lock (_sync)
        {
            return EnumerateCaptureDevices();
        }
    }

    public string? GetDefaultCaptureDeviceId(AudioPolicyRole role)
    {
        ThrowIfDisposed();

        lock (_sync)
        {
            return TryGetDefaultCaptureDevice(role)?.ID;
        }
    }

    public CaptureDeviceInfo? GetCaptureDevice(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ThrowIfDisposed();

        lock (_sync)
        {
            using var device = TryGetDevice(deviceId);
            return device is null ? null : CreateCaptureDeviceInfo(device);
        }
    }

    public void SetCaptureVolume(string deviceId, float volumePercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ThrowIfDisposed();

        var normalizedVolume = Math.Clamp(volumePercent, 0f, 100f) / 100f;

        lock (_sync)
        {
            using var device = TryGetDevice(deviceId) ?? throw new InvalidOperationException($"Capture device '{deviceId}' was not found.");
            device.AudioEndpointVolume.MasterVolumeLevelScalar = normalizedVolume;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

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

        GC.SuppressFinalize(this);
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || _disposed)
        {
            return;
        }

        lock (_sync)
        {
            RefreshVolumeSubscriptions();
        }

        CaptureDeviceStateChanged?.Invoke(this, new CaptureDeviceStateChangedEventArgs(deviceId, MapDeviceState(newState)));
        CaptureDevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        if (_disposed)
        {
            return;
        }

        lock (_sync)
        {
            RefreshVolumeSubscriptions();
        }

        CaptureDevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OnDeviceRemoved(string deviceId)
    {
        if (_disposed)
        {
            return;
        }

        lock (_sync)
        {
            RefreshVolumeSubscriptions();
        }

        CaptureDevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (_disposed || flow != DataFlow.Capture)
        {
            return;
        }

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
        if (_disposed)
        {
            return;
        }

        CaptureDevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    private IReadOnlyList<CaptureDeviceInfo> EnumerateCaptureDevices()
    {
        var collection = _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, AllDeviceStates);

        var defaultDeviceId = TryGetDefaultCaptureDevice(AudioPolicyRole.Multimedia)?.ID;
        var defaultCommunicationsId = TryGetDefaultCaptureDevice(AudioPolicyRole.Communications)?.ID;
        var devices = new List<CaptureDeviceInfo>(collection.Count);

        for (var index = 0; index < collection.Count; index++)
        {
            using var device = collection[index];
            try
            {
                devices.Add(CreateCaptureDeviceInfo(device, defaultDeviceId, defaultCommunicationsId));
            }
            catch
            {
                devices.Add(CreateFallbackCaptureDeviceInfo(device, defaultDeviceId, defaultCommunicationsId));
            }
        }

        return devices;
    }

    private CaptureDeviceInfo CreateCaptureDeviceInfo(MMDevice device, string? defaultDeviceId = null, string? defaultCommunicationsId = null)
    {
        defaultDeviceId ??= TryGetDefaultCaptureDevice(AudioPolicyRole.Multimedia)?.ID;
        defaultCommunicationsId ??= TryGetDefaultCaptureDevice(AudioPolicyRole.Communications)?.ID;
        var (volumePercent, isMuted) = TryReadVolume(device);

        return new CaptureDeviceInfo(
            device.ID,
            TryGetFriendlyName(device),
            MapDeviceState(device.State),
            string.Equals(device.ID, defaultDeviceId, StringComparison.OrdinalIgnoreCase),
            string.Equals(device.ID, defaultCommunicationsId, StringComparison.OrdinalIgnoreCase),
            volumePercent,
            isMuted);
    }

    private CaptureDeviceInfo CreateFallbackCaptureDeviceInfo(MMDevice device, string? defaultDeviceId, string? defaultCommunicationsId)
    {
        return new CaptureDeviceInfo(
            device.ID,
            TryGetFriendlyName(device),
            TryGetDeviceAvailability(device),
            string.Equals(device.ID, defaultDeviceId, StringComparison.OrdinalIgnoreCase),
            string.Equals(device.ID, defaultCommunicationsId, StringComparison.OrdinalIgnoreCase),
            0f,
            false);
    }

    private MMDevice? TryGetDefaultCaptureDevice(AudioPolicyRole role)
    {
        try
        {
            return _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, MapRole(role));
        }
        catch
        {
            return null;
        }
    }

    private MMDevice? TryGetDevice(string deviceId)
    {
        try
        {
            return _enumerator.GetDevice(deviceId);
        }
        catch
        {
            return null;
        }
    }

    private void RefreshVolumeSubscriptions()
    {
        var activeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var collection = _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, ObservableDeviceStates);

        for (var index = 0; index < collection.Count; index++)
        {
            using var device = collection[index];
            activeIds.Add(device.ID);

            if (_observedDevices.ContainsKey(device.ID))
            {
                continue;
            }

            TryAddObservedDevice(device.ID);
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

    private void TryAddObservedDevice(string deviceId)
    {
        try
        {
            var observedDevice = _enumerator.GetDevice(deviceId);
            var endpointVolume = observedDevice.AudioEndpointVolume;
            AudioEndpointVolumeNotificationDelegate handler = data => OnVolumeNotification(observedDevice.ID, data);
            endpointVolume.OnVolumeNotification += handler;

            _observedDevices.Add(observedDevice.ID, observedDevice);
            _volumeHandlers.Add(observedDevice.ID, handler);
        }
        catch
        {
        }
    }

    private static (float VolumePercent, bool IsMuted) TryReadVolume(MMDevice device)
    {
        try
        {
            return (device.AudioEndpointVolume.MasterVolumeLevelScalar * 100f, device.AudioEndpointVolume.Mute);
        }
        catch
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
        catch
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
        catch
        {
            return DeviceAvailability.Unknown;
        }
    }

    private static void TryUnsubscribeVolumeNotification(MMDevice device, AudioEndpointVolumeNotificationDelegate handler)
    {
        try
        {
            device.AudioEndpointVolume.OnVolumeNotification -= handler;
        }
        catch
        {
        }
    }

    private void OnVolumeNotification(string deviceId, AudioVolumeNotificationData data)
    {
        if (_disposed)
        {
            return;
        }

        CaptureDeviceVolumeChanged?.Invoke(
            this,
            new CaptureDeviceVolumeChangedEventArgs(deviceId, data.MasterVolume * 100f, data.Muted));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static Role MapRole(AudioPolicyRole role) =>
        role switch
        {
            AudioPolicyRole.Console => Role.Console,
            AudioPolicyRole.Multimedia => Role.Multimedia,
            AudioPolicyRole.Communications => Role.Communications,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported audio role.")
        };

    private static DeviceAvailability MapDeviceState(DeviceState state) =>
        state switch
        {
            DeviceState.Active => DeviceAvailability.Active,
            DeviceState.Disabled => DeviceAvailability.Disabled,
            DeviceState.NotPresent => DeviceAvailability.NotPresent,
            DeviceState.Unplugged => DeviceAvailability.Unplugged,
            _ => DeviceAvailability.Unknown
        };
}
