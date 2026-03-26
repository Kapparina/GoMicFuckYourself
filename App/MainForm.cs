using GoMicFuckYourself.Contracts.Audio;
using GoMicFuckYourself.Contracts.Configuration;
using GoMicFuckYourself.Contracts.Enforcement;

namespace GoMicFuckYourself.Tray;

public partial class MainForm : Form
{
    private readonly IServicePipeClient _pipeClient;
    private readonly bool _firstRun;
    private bool _allowExit;

    public MainForm(IServicePipeClient pipeClient, bool firstRun)
    {
        _pipeClient = pipeClient;
        _firstRun = firstRun;
        InitializeComponent();
        InitializeTrayBehavior();

        if (_firstRun)
        {
            Text = "GoMicFuckYourself Setup";
            statusLabel.Text = "First-run setup: choose a microphone and apply the policy.";
            Shown += (_, _) => ShowFirstRunNotification();
        }
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await LoadStateAsync();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (WindowState == FormWindowState.Minimized)
        {
            HideToTray();
        }
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

    private async void refreshButton_Click(object? sender, EventArgs e)
    {
        await LoadStateAsync();
    }

    private async void saveButton_Click(object? sender, EventArgs e)
    {
        await SaveAsync(enforceAfterSave: false);
    }

    private async void applyButton_Click(object? sender, EventArgs e)
    {
        await SaveAsync(enforceAfterSave: true);
    }

    private async Task LoadStateAsync()
    {
        await RunBusyAsync(async () =>
        {
            var statusResponse = await _pipeClient.GetStatusAsync();
            var devicesResponse = await _pipeClient.ListCaptureDevicesAsync();
            var configResponse = await _pipeClient.GetConfigAsync();

            if (!statusResponse.Success)
            {
                statusLabel.Text = statusResponse.Error ?? "Failed to load service status.";
            }
            else if (statusResponse.Payload is { } status)
            {
                statusLabel.Text = BuildStatusText(status);
            }

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

            if (configResponse.Success && configResponse.Payload is { } config)
            {
                enforcementEnabledCheckBox.Checked = config.EnforcementEnabled;
                volumeNumericUpDown.Value = Convert.ToDecimal(config.TargetVolumePercent ?? 100f);

                if (!string.IsNullOrWhiteSpace(config.SelectedCaptureDeviceId))
                {
                    devicesComboBox.SelectedValue = config.SelectedCaptureDeviceId;
                }
            }

            if (devicesComboBox.SelectedIndex < 0 && devices.Count > 0)
            {
                devicesComboBox.SelectedIndex = 0;
            }

            if (configResponse.Success)
            {
                errorLabel.Text = string.Empty;
            }
            else
            {
                UpdateError(configResponse.Error ?? "Failed to load config.");
            }
        });
    }

    private async Task SaveAsync(bool enforceAfterSave)
    {
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
                TargetVolumePercent = (float)volumeNumericUpDown.Value,
                EnforcementEnabled = enforcementEnabledCheckBox.Checked
            });

            if (!saveResponse.Success)
            {
                UpdateError(saveResponse.Error ?? "Failed to save config.");
                return;
            }

            if (enforceAfterSave)
            {
                var enforceResponse = await _pipeClient.ForceEnforceAsync();
                if (!enforceResponse.Success)
                {
                    UpdateError(enforceResponse.Error ?? "Failed to enforce config.");
                    return;
                }
            }

            errorLabel.Text = string.Empty;
            statusLabel.Text = enforceAfterSave
                ? "Configuration saved and enforcement triggered."
                : "Configuration saved.";
        });
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
        refreshButton.Enabled = !busy;
        saveButton.Enabled = !busy;
        applyButton.Enabled = !busy;
        devicesComboBox.Enabled = !busy;
        volumeNumericUpDown.Enabled = !busy;
        enforcementEnabledCheckBox.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void UpdateError(string error)
    {
        errorLabel.Text = error;
    }

    private static string BuildStatusText(MicEnforcementStatus status)
    {
        if (!status.IsConfigured)
        {
            return "Service is running. No microphone is configured yet.";
        }

        if (!string.IsNullOrWhiteSpace(status.LastError))
        {
            return $"Configured mic: {status.SelectedCaptureDeviceId}. Last error: {status.LastError}";
        }

        var volumeText = status.TargetVolumePercent is null ? "n/a" : $"{status.TargetVolumePercent:0}%";
        return $"Configured mic: {status.SelectedCaptureDeviceId} | Volume: {volumeText} | Enforcement: {(status.EnforcementEnabled ? "enabled" : "disabled")}";
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
        trayNotifyIcon.BalloonTipText = "The app will stay in the tray after setup so microphone enforcement remains easy to manage.";
        trayNotifyIcon.ShowBalloonTip(4000);
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
        trayNotifyIcon.BalloonTipTitle = "GoMicFuckYourself";
        trayNotifyIcon.BalloonTipText = "Still running in the system tray.";
        trayNotifyIcon.ShowBalloonTip(2500);
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private async void openTrayMenuItem_Click(object? sender, EventArgs e)
    {
        RestoreFromTray();
        await LoadStateAsync();
    }

    private async void refreshTrayMenuItem_Click(object? sender, EventArgs e)
    {
        await LoadStateAsync();
    }

    private void exitTrayMenuItem_Click(object? sender, EventArgs e)
    {
        _allowExit = true;
        Close();
    }

    private void trayNotifyIcon_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            RestoreFromTray();
        }
    }
}
