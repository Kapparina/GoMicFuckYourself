using GoMicFuckYourself.Contracts.Audio;
using GoMicFuckYourself.Contracts.Configuration;
using GoMicFuckYourself.Contracts.Enforcement;
using GoMicFuckYourself.Service.Audio;
using GoMicFuckYourself.Service.Configuration;
using Microsoft.Extensions.Logging;

namespace GoMicFuckYourself.Service.Enforcement;

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

        await _configStore.SaveAsync(normalized, cancellationToken);

        lock (_sync)
        {
            _config = normalized;
        }

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

    public async Task PeriodicEnforceAsync(CancellationToken cancellationToken)
    {
        await EnsureStartedAsync(cancellationToken);
        await EnforceAsync("periodic", cancellationToken);
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

    private void OnCaptureDevicesChanged(object? sender, EventArgs eventArgs) => QueueEnforcement("device-list-change");

    private void OnDefaultCaptureDeviceChanged(object? sender, string deviceId) => QueueEnforcement("default-device-change");

    private void OnDefaultCommunicationsDeviceChanged(object? sender, string deviceId) => QueueEnforcement("default-communications-change");

    private void OnCaptureDeviceStateChanged(object? sender, CaptureDeviceStateChangedEventArgs eventArgs)
    {
        if (string.Equals(eventArgs.DeviceId, GetSelectedDeviceId(), StringComparison.OrdinalIgnoreCase))
        {
            QueueEnforcement("selected-device-state-change");
        }
    }

    private void OnCaptureDeviceVolumeChanged(object? sender, CaptureDeviceVolumeChangedEventArgs eventArgs)
    {
        if (string.Equals(eventArgs.DeviceId, GetSelectedDeviceId(), StringComparison.OrdinalIgnoreCase))
        {
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
                SetLastResult(DateTimeOffset.UtcNow, null);
                return;
            }

            if (string.IsNullOrWhiteSpace(config.SelectedCaptureDeviceId))
            {
                SetLastResult(DateTimeOffset.UtcNow, null);
                return;
            }

            var device = _audioController.GetCaptureDevice(config.SelectedCaptureDeviceId);
            if (device is null)
            {
                SetLastResult(null, $"Configured capture device '{config.SelectedCaptureDeviceId}' was not found.");
                return;
            }

            if (device.State != DeviceAvailability.Active)
            {
                SetLastResult(null, $"Configured capture device '{config.SelectedCaptureDeviceId}' is not active.");
                return;
            }

            EnsureDefaultEndpoint(config.SelectedCaptureDeviceId, AudioPolicyRole.Console);
            EnsureDefaultEndpoint(config.SelectedCaptureDeviceId, AudioPolicyRole.Multimedia);
            EnsureDefaultEndpoint(config.SelectedCaptureDeviceId, AudioPolicyRole.Communications);

            if (config.TargetVolumePercent is { } targetVolume &&
                Math.Abs(device.VolumePercent - targetVolume) > 0.5f)
            {
                _audioController.SetCaptureVolume(config.SelectedCaptureDeviceId, targetVolume);
            }

            SetLastResult(DateTimeOffset.UtcNow, null);
            _logger.LogDebug("Enforced microphone policy for {DeviceId} after {Reason}.", config.SelectedCaptureDeviceId, reason);
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
}
