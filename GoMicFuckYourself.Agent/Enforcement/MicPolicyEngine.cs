using GoMicFuckYourself.Contracts.Audio;
using GoMicFuckYourself.Contracts.Configuration;
using GoMicFuckYourself.Contracts.Enforcement;
using GoMicFuckYourself.Agent.Audio;
using GoMicFuckYourself.Agent.Configuration;

namespace GoMicFuckYourself.Agent.Enforcement;

public sealed class MicPolicyEngine : IMicPolicyEngine, IDisposable
{
    private readonly IAudioController _audioController;
    private readonly IPolicyConfigInterop _policyConfigInterop;
    private readonly IConfigStore _configStore;
    private readonly ILogger<MicPolicyEngine> _logger;
    private readonly SemaphoreSlim _enforcementLock = new(1, 1);
    private readonly Lock _sync = new();

    private ServiceConfig _config = new();
    private DateTimeOffset? _lastEnforcementUtc;
    private string? _lastError;
    private bool _started;
    private bool _subscribed;
    private bool _disposed;

    public MicPolicyEngine(
        IAudioController audioController,
        IPolicyConfigInterop policyConfigInterop,
        IConfigStore configStore,
        ILogger<MicPolicyEngine> logger)
    {
        _audioController = audioController;
        _policyConfigInterop = policyConfigInterop;
        _configStore = configStore;
        _logger = logger;
    }

    public MicEnforcementStatus GetStatus()
    {
        lock (_sync)
        {
            return new MicEnforcementStatus(
                !string.IsNullOrWhiteSpace(_config.SelectedCaptureDeviceId),
                _config.EnforcementEnabled,
                _config.SelectedCaptureDeviceId,
                _config.TargetVolumePercent,
                _lastEnforcementUtc,
                _lastError);
        }
    }

    public async Task<ServiceConfig> GetConfigAsync(CancellationToken cancellationToken)
    {
        await EnsureStartedAsync(cancellationToken);

        lock (_sync)
        {
            return CloneConfig(_config);
        }
    }

    public async Task SaveConfigAsync(ServiceConfig config, CancellationToken cancellationToken)
    {
        var normalized = NormalizeConfig(config);
        var previous = GetConfigSnapshot();

        await _configStore.SaveAsync(normalized, cancellationToken);

        lock (_sync)
        {
            _config = normalized;
        }

        LogConfigChange(previous, normalized);

        await EnforceAsync("config-save", cancellationToken);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureStartedAsync(cancellationToken);
        EnsureSubscribed();
        await EnforceAsync("startup", cancellationToken);
    }

    public async Task ForceEnforceAsync(CancellationToken cancellationToken)
    {
        await EnsureStartedAsync(cancellationToken);
        await EnforceAsync("forced", cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_subscribed)
        {
            _audioController.CaptureDevicesChanged -= OnCaptureDevicesChanged;
            _audioController.DefaultCaptureDeviceChanged -= OnDefaultCaptureDeviceChanged;
            _audioController.DefaultCommunicationsDeviceChanged -= OnDefaultCommunicationsDeviceChanged;
            _audioController.CaptureDeviceStateChanged -= OnCaptureDeviceStateChanged;
            _audioController.CaptureDeviceVolumeChanged -= OnCaptureDeviceVolumeChanged;
        }

        _enforcementLock.Dispose();
        _disposed = true;
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_started)
        {
            return;
        }

        var config = NormalizeConfig(await _configStore.LoadAsync(cancellationToken));

        lock (_sync)
        {
            if (_started)
            {
                return;
            }

            _config = config;
            _started = true;
        }
    }

    private void EnsureSubscribed()
    {
        if (_subscribed)
        {
            return;
        }

        _audioController.CaptureDevicesChanged += OnCaptureDevicesChanged;
        _audioController.DefaultCaptureDeviceChanged += OnDefaultCaptureDeviceChanged;
        _audioController.DefaultCommunicationsDeviceChanged += OnDefaultCommunicationsDeviceChanged;
        _audioController.CaptureDeviceStateChanged += OnCaptureDeviceStateChanged;
        _audioController.CaptureDeviceVolumeChanged += OnCaptureDeviceVolumeChanged;
        _subscribed = true;
    }

    private void OnCaptureDevicesChanged(object? sender, EventArgs eventArgs)
    {
        _logger.LogInformation("Detected capture device list change.");
        QueueEnforcement("device-list-change");
    }

    private void OnDefaultCaptureDeviceChanged(object? sender, string deviceId)
    {
        _logger.LogWarning(
            "Detected default capture device change to {DeviceId}. Windows Core Audio callbacks do not expose the originating process.",
            deviceId);
        QueueEnforcement("default-device-change");
    }

    private void OnDefaultCommunicationsDeviceChanged(object? sender, string deviceId)
    {
        _logger.LogWarning(
            "Detected default communications capture device change to {DeviceId}. Windows Core Audio callbacks do not expose the originating process.",
            deviceId);
        QueueEnforcement("default-communications-change");
    }

    private void OnCaptureDeviceStateChanged(object? sender, CaptureDeviceStateChangedEventArgs eventArgs)
    {
        if (string.Equals(eventArgs.DeviceId, GetSelectedDeviceId(), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Detected state change for selected capture device {DeviceId}. New state: {State}.",
                eventArgs.DeviceId,
                eventArgs.State);
            QueueEnforcement("selected-device-state-change");
        }
    }

    private void OnCaptureDeviceVolumeChanged(object? sender, CaptureDeviceVolumeChangedEventArgs eventArgs)
    {
        if (string.Equals(eventArgs.DeviceId, GetSelectedDeviceId(), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Detected volume change for selected capture device {DeviceId}. New volume: {VolumePercent}. Muted: {IsMuted}. Windows Core Audio callbacks do not expose the originating process.",
                eventArgs.DeviceId,
                eventArgs.VolumePercent,
                eventArgs.IsMuted);
            QueueEnforcement("selected-device-volume-change");
        }
    }

    private void QueueEnforcement(string reason)
    {
        if (_disposed)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await EnforceAsync(reason, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Queued enforcement failed after {Reason}.", reason);
            }
        });
    }

    private async Task EnforceAsync(string reason, CancellationToken cancellationToken)
    {
        await _enforcementLock.WaitAsync(cancellationToken);
        try
        {
            var config = GetConfigSnapshot();

            if (!config.EnforcementEnabled)
            {
                _logger.LogInformation("Skipped microphone enforcement after {Reason} because enforcement is disabled.", reason);
                SetLastResult(DateTimeOffset.UtcNow, null);
                return;
            }

            if (string.IsNullOrWhiteSpace(config.SelectedCaptureDeviceId))
            {
                _logger.LogInformation("Skipped microphone enforcement after {Reason} because no microphone is configured.", reason);
                SetLastResult(DateTimeOffset.UtcNow, null);
                return;
            }

            var device = _audioController.GetCaptureDevice(config.SelectedCaptureDeviceId);
            if (device is null)
            {
                _logger.LogError("Configured capture device {DeviceId} was not found during enforcement after {Reason}.", config.SelectedCaptureDeviceId, reason);
                SetLastResult(null, $"Configured capture device '{config.SelectedCaptureDeviceId}' was not found.");
                return;
            }

            if (device.State != DeviceAvailability.Active)
            {
                _logger.LogError("Configured capture device {DeviceId} is not active during enforcement after {Reason}. Current state: {State}.", config.SelectedCaptureDeviceId, reason, device.State);
                SetLastResult(null, $"Configured capture device '{config.SelectedCaptureDeviceId}' is not active.");
                return;
            }

            EnsureDefaultEndpoint(config.SelectedCaptureDeviceId, AudioPolicyRole.Console);
            EnsureDefaultEndpoint(config.SelectedCaptureDeviceId, AudioPolicyRole.Multimedia);
            EnsureDefaultEndpoint(config.SelectedCaptureDeviceId, AudioPolicyRole.Communications);

            if (config.TargetVolumePercent is { } targetVolume &&
                Math.Abs(device.VolumePercent - targetVolume) > 0.5f)
            {
                _logger.LogInformation(
                    "Reverting capture volume for {DeviceId} from {CurrentVolumePercent} to {TargetVolumePercent} after {Reason}.",
                    config.SelectedCaptureDeviceId,
                    device.VolumePercent,
                    targetVolume,
                    reason);
                _audioController.SetCaptureVolume(config.SelectedCaptureDeviceId, targetVolume);
            }

            SetLastResult(DateTimeOffset.UtcNow, null);
            _logger.LogInformation("Enforced microphone policy for {DeviceId} after {Reason}.", config.SelectedCaptureDeviceId, reason);
        }
        catch (Exception exception)
        {
            SetLastResult(null, exception.Message);
            _logger.LogError(exception, "Microphone enforcement failed after {Reason}.", reason);
            throw;
        }
        finally
        {
            _enforcementLock.Release();
        }
    }

    private void EnsureDefaultEndpoint(string selectedDeviceId, AudioPolicyRole role)
    {
        var currentDeviceId = _audioController.GetDefaultCaptureDeviceId(role);
        if (!string.Equals(currentDeviceId, selectedDeviceId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Reverting {Role} default capture device from {CurrentDeviceId} to {SelectedDeviceId}.",
                role,
                currentDeviceId ?? "<none>",
                selectedDeviceId);
            _policyConfigInterop.SetDefaultEndpoint(selectedDeviceId, role);
        }
    }

    private ServiceConfig GetConfigSnapshot()
    {
        lock (_sync)
        {
            return CloneConfig(_config);
        }
    }

    private string? GetSelectedDeviceId()
    {
        lock (_sync)
        {
            return _config.SelectedCaptureDeviceId;
        }
    }

    private void SetLastResult(DateTimeOffset? enforcedAtUtc, string? error)
    {
        lock (_sync)
        {
            if (enforcedAtUtc is not null)
            {
                _lastEnforcementUtc = enforcedAtUtc;
            }

            _lastError = error;
        }
    }

    private static ServiceConfig NormalizeConfig(ServiceConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new ServiceConfig
        {
            SelectedCaptureDeviceId = string.IsNullOrWhiteSpace(config.SelectedCaptureDeviceId)
                ? null
                : config.SelectedCaptureDeviceId.Trim(),
            TargetVolumePercent = config.TargetVolumePercent is null
                ? null
                : Math.Clamp(config.TargetVolumePercent.Value, 0f, 100f),
            EnforcementEnabled = config.EnforcementEnabled
        };
    }

    private static ServiceConfig CloneConfig(ServiceConfig config)
    {
        return new ServiceConfig
        {
            SelectedCaptureDeviceId = config.SelectedCaptureDeviceId,
            TargetVolumePercent = config.TargetVolumePercent,
            EnforcementEnabled = config.EnforcementEnabled
        };
    }

    private void LogConfigChange(ServiceConfig previous, ServiceConfig current)
    {
        if (string.Equals(previous.SelectedCaptureDeviceId, current.SelectedCaptureDeviceId, StringComparison.OrdinalIgnoreCase) &&
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            previous.TargetVolumePercent == current.TargetVolumePercent &&
            previous.EnforcementEnabled == current.EnforcementEnabled)
        {
            _logger.LogInformation("Received config save request, but the configuration did not change.");
            return;
        }

        _logger.LogInformation(
            "Config changed. Device: {PreviousDeviceId} -> {CurrentDeviceId}; Volume: {PreviousVolumePercent} -> {CurrentVolumePercent}; Enforcement: {PreviousEnforcementEnabled} -> {CurrentEnforcementEnabled}.",
            previous.SelectedCaptureDeviceId ?? "<none>",
            current.SelectedCaptureDeviceId ?? "<none>",
            previous.TargetVolumePercent?.ToString("0.##") ?? "<unset>",
            current.TargetVolumePercent?.ToString("0.##") ?? "<unset>",
            previous.EnforcementEnabled,
            current.EnforcementEnabled);
    }
}
