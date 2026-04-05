using GoMicFuckYourself.Contracts.Audio;
using GoMicFuckYourself.Contracts.Configuration;
using GoMicFuckYourself.Contracts.Enforcement;
using GoMicFuckYourself.Tray.Models;

namespace GoMicFuckYourself.Tray;

public partial class MainForm : Form
{
    private readonly bool _firstRun;
    private readonly IAgentPipeClient _pipeClient;
    private bool _allowExit;
    private bool _isBusy;
    private bool _isDirty;
    private bool _isLoadingState;
    private ServiceConfig? _loadedConfig;
    private bool _loadedStartOnLoginEnabled;
    private bool _suppressInitialShow;

    public MainForm(IAgentPipeClient pipeClient, bool firstRun)
    {
        _pipeClient = pipeClient;
        _firstRun = firstRun;
        _suppressInitialShow = !firstRun;
        InitializeComponent();
        InitializeTrayBehavior();
        HookDirtyTracking();
        UpdateActionButtons();

        if (_firstRun) Shown += (_, _) => ShowFirstRunNotification();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_firstRun)
        {
            Text = "GoMicFuckYourself Setup";
            statusLabel.Text = "First-run setup: choose a microphone and apply the policy.";
        }

        await LoadStateAsync();

        if (_firstRun) BringToFrontForSetup();
    }

    protected override void SetVisibleCore(bool value)
    {
        if (_suppressInitialShow && !IsHandleCreated) CreateHandle();

        if (_suppressInitialShow && value)
        {
            _suppressInitialShow = false;
            value = false;
            ShowInTaskbar = false;
        }

        base.SetVisibleCore(value);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (WindowState == FormWindowState.Minimized) HideToTray();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        trayNotifyIcon.Visible = false;
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (SingleInstanceSignal.IsActivationMessage(m))
        {
            BeginInvoke(new Action(async () =>
            {
                RestoreFromTray();
                await LoadStateAsync();
            }));
            return;
        }

        base.WndProc(ref m);
    }

    private async void refreshButton_Click(object? sender, EventArgs e)
    {
        await LoadStateAsync();
    }

    private async void saveButton_Click(object? sender, EventArgs e)
    {
        await SaveAsync(false);
    }

    private async void applyButton_Click(object? sender, EventArgs e)
    {
        await SaveAsync(true);
    }

    private async void restartAgentButton_Click(object? sender, EventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            ProcessCoordinator.RestartAgent();

            var isReady = await AgentProcess.EnsureAgentReadyAsync(CancellationToken.None, false);
            if (!isReady)
            {
                UpdateError("The agent did not restart in time.");
                return;
            }

            await LoadStateAsync();
            statusLabel.Text = "The enforcement agent was restarted.";
        });
    }

    private async Task LoadStateAsync()
    {
        await RunBusyAsync(async () =>
        {
            BeginStateLoad();

            try
            {
                var (statusResponse, devicesResponse, configResponse) = await LoadAgentStateAsync();

                if (!statusResponse.Success)
                    statusLabel.Text = statusResponse.Error ?? "Failed to load agent status.";
                else if (statusResponse.Payload is { } status) statusLabel.Text = BuildStatusText(status);

                if (!devicesResponse.Success)
                {
                    devicesComboBox.DataSource = null;
                    UpdateError(devicesResponse.Error ?? "Failed to load devices.");
                    return;
                }

                var devices = devicesResponse.Payload ?? [];
                devicesComboBox.DataSource = devices;
                devicesComboBox.DisplayMember = nameof(CaptureDeviceInfo.FriendlyName);
                devicesComboBox.ValueMember = nameof(CaptureDeviceInfo.Id);
                var startupState = AutorunRegistry.GetStartupStateForCurrentUser();
                _loadedStartOnLoginEnabled = startupState ?? true;
                startOnLoginCheckBox.Checked = _loadedStartOnLoginEnabled;

                if (configResponse.Success && configResponse.Payload is { } config)
                {
                    _loadedConfig = config;
                    enforcementEnabledCheckBox.Checked = config.EnforcementEnabled;
                    volumeNumericUpDown.Value = Convert.ToDecimal(config.TargetVolumePercent ?? 100f);

                    if (!string.IsNullOrWhiteSpace(config.SelectedCaptureDeviceId))
                        devicesComboBox.SelectedValue = config.SelectedCaptureDeviceId;
                }
                else
                {
                    _loadedConfig = new ServiceConfig();
                }

                if (devicesComboBox.SelectedIndex < 0 && devices.Count > 0) devicesComboBox.SelectedIndex = 0;

                if (string.IsNullOrWhiteSpace(_loadedConfig?.SelectedCaptureDeviceId) &&
                    devicesComboBox.SelectedItem is CaptureDeviceInfo defaultDevice)
                {
                    var clampedVolume = Math.Clamp(defaultDevice.VolumePercent, 0f, 100f);
                    volumeNumericUpDown.Value = Convert.ToDecimal(clampedVolume);
                }

                if (configResponse.Success)
                    errorLabel.Text = string.Empty;
                else
                    UpdateError(configResponse.Error ?? "Failed to load config.");

                UpdateCurrentReadout(statusResponse.Payload, devices, configResponse.Payload);
            }
            finally
            {
                EndStateLoad();
            }
        });
    }

    private async Task<(
        PipeResponse<MicEnforcementStatus> Status,
        PipeResponse<List<CaptureDeviceInfo>> Devices,
        PipeResponse<ServiceConfig> Config)> LoadAgentStateAsync()
    {
        var isReady = await AgentProcess.EnsureAgentReadyAsync(CancellationToken.None, true);
        if (!isReady) throw new TimeoutException("The agent did not start in time.");

        return await QueryAgentStateAsync();
    }

    private async Task<(
        PipeResponse<MicEnforcementStatus> Status,
        PipeResponse<List<CaptureDeviceInfo>> Devices,
        PipeResponse<ServiceConfig> Config)> QueryAgentStateAsync()
    {
        var statusResponse = await _pipeClient.GetStatusAsync();
        var devicesResponse = await _pipeClient.ListCaptureDevicesAsync();
        var configResponse = await _pipeClient.GetConfigAsync();
        return (statusResponse, devicesResponse, configResponse);
    }

    private async Task SaveAsync(bool closeAfterSave)
    {
        var savedSuccessfully = false;
        string? successMessage = null;

        await RunBusyAsync(async () =>
        {
            if (devicesComboBox.SelectedItem is not CaptureDeviceInfo selectedDevice)
            {
                UpdateError("Select a capture device first.");
                return;
            }

            var saveResponse = await _pipeClient.SaveConfigAsync(new ServiceConfig
            {
                SelectedCaptureDeviceId = selectedDevice.Id,
                SelectedCaptureDeviceName = selectedDevice.FriendlyName,
                TargetVolumePercent = (float)volumeNumericUpDown.Value,
                EnforcementEnabled = enforcementEnabledCheckBox.Checked
            });

            if (!saveResponse.Success)
            {
                UpdateError(saveResponse.Error ?? "Failed to save config.");
                return;
            }

            if (_firstRun)
            {
                AutorunRegistry.SetForCurrentUser(startOnLoginCheckBox.Checked);
                ProcessCoordinator.RestartAgent();

                var isReady = await AgentProcess.EnsureAgentReadyAsync(CancellationToken.None, false);
                if (!isReady)
                {
                    UpdateError("Configuration was saved, but the agent did not restart in time.");
                    return;
                }
            }
            else
            {
                AutorunRegistry.SetForCurrentUser(startOnLoginCheckBox.Checked);
            }

            var enforceResponse = await _pipeClient.ForceEnforceAsync();
            if (!enforceResponse.Success)
            {
                UpdateError(enforceResponse.Error ?? "Failed to enforce config.");
                return;
            }

            errorLabel.Text = string.Empty;
            _loadedStartOnLoginEnabled = startOnLoginCheckBox.Checked;
            SetDirty(false);
            successMessage = _firstRun
                ? "Configuration saved and startup behavior was updated."
                : "Configuration saved and enforcement triggered.";
            savedSuccessfully = true;
        });

        if (!savedSuccessfully) return;

        await LoadStateAsync();

        if (!string.IsNullOrWhiteSpace(successMessage)) statusLabel.Text = successMessage;

        if (closeAfterSave) HideToTray();
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        SetBusy(true);
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            UpdateError(exception.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        refreshButton.Enabled = !busy;
        devicesComboBox.Enabled = !busy;
        volumeNumericUpDown.Enabled = !busy;
        enforcementEnabledCheckBox.Enabled = !busy;
        startOnLoginCheckBox.Enabled = !busy;
        restartAgentButton.Enabled = !busy;
        UpdateActionButtons();
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void UpdateError(string error)
    {
        errorLabel.Text = error;
    }

    private void HookDirtyTracking()
    {
        devicesComboBox.SelectedIndexChanged += (_, _) => HandleSelectedDeviceChanged();
        volumeNumericUpDown.ValueChanged += (_, _) => MarkDirtyFromUserInput();
        volumeNumericUpDown.TextChanged += (_, _) => MarkDirtyFromUserInput();
        enforcementEnabledCheckBox.CheckedChanged += (_, _) => MarkDirtyFromUserInput();
        startOnLoginCheckBox.CheckedChanged += (_, _) => MarkDirtyFromUserInput();
    }

    private void HandleSelectedDeviceChanged()
    {
        if (_isLoadingState) return;

        if (devicesComboBox.SelectedItem is CaptureDeviceInfo selectedDevice)
        {
            var clampedVolume = Math.Clamp(selectedDevice.VolumePercent, 0f, 100f);
            volumeNumericUpDown.Value = Convert.ToDecimal(clampedVolume);
        }

        RefreshDirtyState();
    }

    private void MarkDirtyFromUserInput()
    {
        if (_isLoadingState) return;

        RefreshDirtyState();
    }

    private void SetDirty(bool isDirty)
    {
        _isDirty = isDirty;
        UpdateActionButtons();
    }

    private void UpdateActionButtons()
    {
        var enableSaveActions = !_isBusy && _isDirty;
        saveButton.Enabled = enableSaveActions;
        applyButton.Enabled = enableSaveActions;
    }

    private void BeginStateLoad()
    {
        _isLoadingState = true;
    }

    private void EndStateLoad()
    {
        _isLoadingState = false;
        RefreshDirtyState();
    }

    private void RefreshDirtyState()
    {
        if (_isLoadingState) return;

        var currentDeviceId = (devicesComboBox.SelectedItem as CaptureDeviceInfo)?.Id;
        var currentVolume = (float)volumeNumericUpDown.Value;
        var currentEnforcementEnabled = enforcementEnabledCheckBox.Checked;
        var currentStartOnLoginEnabled = startOnLoginCheckBox.Checked;

        var loadedDeviceId = _loadedConfig?.SelectedCaptureDeviceId;
        var loadedVolume = _loadedConfig?.TargetVolumePercent ?? 100f;
        var loadedEnforcementEnabled = _loadedConfig?.EnforcementEnabled ?? true;

        var isDirty =
            !string.Equals(currentDeviceId, loadedDeviceId, StringComparison.OrdinalIgnoreCase) ||
            Math.Abs(currentVolume - loadedVolume) > 0.01f ||
            currentEnforcementEnabled != loadedEnforcementEnabled ||
            currentStartOnLoginEnabled != _loadedStartOnLoginEnabled;

        SetDirty(isDirty);
    }

    private static string BuildStatusText(MicEnforcementStatus status)
    {
        if (!status.IsConfigured) return "Agent is running. No microphone is configured yet.";

        if (!string.IsNullOrWhiteSpace(status.LastError))
            return $"Configured mic: {status.SelectedCaptureDeviceId}. Last error: {status.LastError}";

        var volumeText = status.TargetVolumePercent is null ? "n/a" : $"{status.TargetVolumePercent:0}%";
        return
            $"Configured mic: {status.SelectedCaptureDeviceId} | Volume: {volumeText} | Enforcement: {(status.EnforcementEnabled ? "enabled" : "disabled")}";
    }

    private void UpdateCurrentReadout(
        MicEnforcementStatus? status,
        IReadOnlyList<CaptureDeviceInfo> devices,
        ServiceConfig? config)
    {
        var selectedDeviceId = config?.SelectedCaptureDeviceId ?? status?.SelectedCaptureDeviceId;
        var selectedDevice = devices.FirstOrDefault(device =>
            string.Equals(device.Id, selectedDeviceId, StringComparison.OrdinalIgnoreCase));

        currentMicValueLabel.Text = selectedDevice?.FriendlyName
                                    ?? (string.IsNullOrWhiteSpace(selectedDeviceId)
                                        ? "Not configured"
                                        : selectedDeviceId);

        currentVolumeValueLabel.Text = selectedDevice is null
            ? "n/a"
            : $"{selectedDevice.VolumePercent:0}%";
    }

    private void InitializeTrayBehavior()
    {
        trayNotifyIcon.Text = "GoMicFuckYourself";
        trayNotifyIcon.Visible = true;
        trayNotifyIcon.Icon = Icon;
    }

    private void ShowFirstRunNotification()
    {
        trayNotifyIcon.BalloonTipTitle = "GoMicFuckYourself";
        trayNotifyIcon.BalloonTipText =
            "The app will stay in the tray after setup so microphone enforcement remains easy to manage.";
        trayNotifyIcon.ShowBalloonTip(4000);
    }

    private void HideToTray(bool showBalloonTip = true)
    {
        Opacity = 1;
        Hide();
        ShowInTaskbar = false;
        if (showBalloonTip)
        {
            trayNotifyIcon.BalloonTipTitle = "GoMicFuckYourself";
            trayNotifyIcon.BalloonTipText = "Still running in the system tray.";
            trayNotifyIcon.ShowBalloonTip(2500);
        }
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void BringToFrontForSetup()
    {
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Show();
        BringToFront();
        Activate();
        TopMost = true;
        TopMost = false;
        Focus();
    }

    private async void openTrayMenuItem_Click(object? sender, EventArgs e)
    {
        RestoreFromTray();
        await LoadStateAsync();
    }

    private void exitTrayMenuItem_Click(object? sender, EventArgs e)
    {
        _allowExit = true;
        ProcessCoordinator.TerminateAgentInstances();
        Close();
    }

    private void trayNotifyIcon_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) RestoreFromTray();
    }
}
