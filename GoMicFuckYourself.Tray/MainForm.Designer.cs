namespace GoMicFuckYourself.Tray;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private NotifyIcon trayNotifyIcon;
    private ContextMenuStrip trayContextMenuStrip;
    private ToolStripMenuItem openTrayMenuItem;
    private ToolStripSeparator traySeparator;
    private ToolStripMenuItem exitTrayMenuItem;
    private Label titleLabel;
    private Label currentMicCaptionLabel;
    private Label currentMicValueLabel;
    private Label currentVolumeCaptionLabel;
    private Label currentVolumeValueLabel;
    private Label deviceLabel;
    private ComboBox devicesComboBox;
    private Label volumeLabel;
    private NumericUpDown volumeNumericUpDown;
    private CheckBox enforcementEnabledCheckBox;
    private CheckBox startOnLoginCheckBox;
    private Button refreshButton;
    private Button saveButton;
    private Button applyButton;
    private Button restartAgentButton;
    private Label statusLabel;
    private Label errorLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        trayContextMenuStrip = new ContextMenuStrip(components);
        openTrayMenuItem = new ToolStripMenuItem();
        traySeparator = new ToolStripSeparator();
        exitTrayMenuItem = new ToolStripMenuItem();
        trayNotifyIcon = new NotifyIcon(components);
        titleLabel = new Label();
        currentMicCaptionLabel = new Label();
        currentMicValueLabel = new Label();
        currentVolumeCaptionLabel = new Label();
        currentVolumeValueLabel = new Label();
        deviceLabel = new Label();
        devicesComboBox = new ComboBox();
        volumeLabel = new Label();
        volumeNumericUpDown = new NumericUpDown();
        enforcementEnabledCheckBox = new CheckBox();
        startOnLoginCheckBox = new CheckBox();
        refreshButton = new Button();
        saveButton = new Button();
        applyButton = new Button();
        restartAgentButton = new Button();
        statusLabel = new Label();
        errorLabel = new Label();
        ((System.ComponentModel.ISupportInitialize)volumeNumericUpDown).BeginInit();
        trayContextMenuStrip.SuspendLayout();
        SuspendLayout();
        // 
        // trayContextMenuStrip
        // 
        trayContextMenuStrip.Items.AddRange(new ToolStripItem[] { openTrayMenuItem, traySeparator, exitTrayMenuItem });
        trayContextMenuStrip.Name = "trayContextMenuStrip";
        trayContextMenuStrip.Size = new Size(104, 54);
        // 
        // openTrayMenuItem
        // 
        openTrayMenuItem.Name = "openTrayMenuItem";
        openTrayMenuItem.Size = new Size(113, 22);
        openTrayMenuItem.Text = "Open";
        openTrayMenuItem.Click += openTrayMenuItem_Click;
        // 
        // traySeparator
        // 
        traySeparator.Name = "traySeparator";
        traySeparator.Size = new Size(100, 6);
        // 
        // exitTrayMenuItem
        // 
        exitTrayMenuItem.Name = "exitTrayMenuItem";
        exitTrayMenuItem.Size = new Size(103, 22);
        exitTrayMenuItem.Text = "Exit";
        exitTrayMenuItem.Click += exitTrayMenuItem_Click;
        // 
        // trayNotifyIcon
        // 
        trayNotifyIcon.ContextMenuStrip = trayContextMenuStrip;
        trayNotifyIcon.Text = "GoMicFuckYourself";
        trayNotifyIcon.Visible = true;
        trayNotifyIcon.MouseDoubleClick += trayNotifyIcon_MouseDoubleClick;
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        titleLabel.Location = new Point(24, 20);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(301, 25);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Microphone enforcement settings";
        currentMicCaptionLabel.AutoSize = true;
        currentMicCaptionLabel.Location = new Point(24, 58);
        currentMicCaptionLabel.Name = "currentMicCaptionLabel";
        currentMicCaptionLabel.Size = new Size(71, 15);
        currentMicCaptionLabel.TabIndex = 1;
        currentMicCaptionLabel.Text = "Current mic:";
        currentMicValueLabel.AutoSize = true;
        currentMicValueLabel.Location = new Point(101, 58);
        currentMicValueLabel.Name = "currentMicValueLabel";
        currentMicValueLabel.Size = new Size(97, 15);
        currentMicValueLabel.TabIndex = 2;
        currentMicValueLabel.Text = "Not configured";
        currentVolumeCaptionLabel.AutoSize = true;
        currentVolumeCaptionLabel.Location = new Point(344, 58);
        currentVolumeCaptionLabel.Name = "currentVolumeCaptionLabel";
        currentVolumeCaptionLabel.Size = new Size(88, 15);
        currentVolumeCaptionLabel.TabIndex = 3;
        currentVolumeCaptionLabel.Text = "Current volume:";
        currentVolumeValueLabel.AutoSize = true;
        currentVolumeValueLabel.Location = new Point(438, 58);
        currentVolumeValueLabel.Name = "currentVolumeValueLabel";
        currentVolumeValueLabel.Size = new Size(25, 15);
        currentVolumeValueLabel.TabIndex = 4;
        currentVolumeValueLabel.Text = "n/a";
        deviceLabel.AutoSize = true;
        deviceLabel.Location = new Point(24, 90);
        deviceLabel.Name = "deviceLabel";
        deviceLabel.Size = new Size(116, 15);
        deviceLabel.TabIndex = 5;
        deviceLabel.Text = "Capture microphone";
        devicesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        devicesComboBox.FormattingEnabled = true;
        devicesComboBox.Location = new Point(24, 108);
        devicesComboBox.Name = "devicesComboBox";
        devicesComboBox.Size = new Size(520, 23);
        devicesComboBox.TabIndex = 6;
        volumeLabel.AutoSize = true;
        volumeLabel.Location = new Point(24, 152);
        volumeLabel.Name = "volumeLabel";
        volumeLabel.Size = new Size(80, 15);
        volumeLabel.TabIndex = 7;
        volumeLabel.Text = "Target volume";
        volumeNumericUpDown.Location = new Point(24, 170);
        volumeNumericUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        volumeNumericUpDown.Name = "volumeNumericUpDown";
        volumeNumericUpDown.Size = new Size(120, 23);
        volumeNumericUpDown.TabIndex = 8;
        volumeNumericUpDown.Value = new decimal(new int[] { 100, 0, 0, 0 });
        enforcementEnabledCheckBox.AutoSize = true;
        enforcementEnabledCheckBox.Checked = true;
        enforcementEnabledCheckBox.CheckState = CheckState.Checked;
        enforcementEnabledCheckBox.Location = new Point(24, 213);
        enforcementEnabledCheckBox.Name = "enforcementEnabledCheckBox";
        enforcementEnabledCheckBox.Size = new Size(192, 19);
        enforcementEnabledCheckBox.TabIndex = 9;
        enforcementEnabledCheckBox.Text = "Enable continuous enforcement";
        enforcementEnabledCheckBox.UseVisualStyleBackColor = true;
        startOnLoginCheckBox.AutoSize = true;
        startOnLoginCheckBox.Checked = true;
        startOnLoginCheckBox.CheckState = CheckState.Checked;
        startOnLoginCheckBox.Location = new Point(24, 238);
        startOnLoginCheckBox.Name = "startOnLoginCheckBox";
        startOnLoginCheckBox.Size = new Size(220, 19);
        startOnLoginCheckBox.TabIndex = 10;
        startOnLoginCheckBox.Text = "Start automatically when I sign in";
        startOnLoginCheckBox.UseVisualStyleBackColor = true;
        refreshButton.Location = new Point(24, 259);
        refreshButton.Name = "refreshButton";
        refreshButton.Size = new Size(90, 32);
        refreshButton.TabIndex = 11;
        refreshButton.Text = "Refresh";
        refreshButton.UseVisualStyleBackColor = true;
        refreshButton.Click += refreshButton_Click;
        saveButton.Location = new Point(120, 259);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(90, 32);
        saveButton.TabIndex = 12;
        saveButton.Text = "Save";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += saveButton_Click;
        applyButton.Location = new Point(216, 259);
        applyButton.Name = "applyButton";
        applyButton.Size = new Size(120, 32);
        applyButton.TabIndex = 13;
        applyButton.Text = "Save and close";
        applyButton.UseVisualStyleBackColor = true;
        applyButton.Click += applyButton_Click;
        restartAgentButton.Location = new Point(342, 259);
        restartAgentButton.Name = "restartAgentButton";
        restartAgentButton.Size = new Size(202, 32);
        restartAgentButton.TabIndex = 14;
        restartAgentButton.Text = "Reboot Enforcement Agent";
        restartAgentButton.UseVisualStyleBackColor = true;
        restartAgentButton.Click += restartAgentButton_Click;
        statusLabel.Location = new Point(24, 311);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(520, 42);
        statusLabel.TabIndex = 15;
        statusLabel.Text = "Loading agent state...";
        errorLabel.ForeColor = Color.Firebrick;
        errorLabel.Location = new Point(24, 355);
        errorLabel.Name = "errorLabel";
        errorLabel.Size = new Size(520, 48);
        errorLabel.TabIndex = 16;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(584, 411);
        Controls.Add(errorLabel);
        Controls.Add(statusLabel);
        Controls.Add(restartAgentButton);
        Controls.Add(applyButton);
        Controls.Add(saveButton);
        Controls.Add(refreshButton);
        Controls.Add(startOnLoginCheckBox);
        Controls.Add(enforcementEnabledCheckBox);
        Controls.Add(volumeNumericUpDown);
        Controls.Add(volumeLabel);
        Controls.Add(devicesComboBox);
        Controls.Add(deviceLabel);
        Controls.Add(currentVolumeValueLabel);
        Controls.Add(currentVolumeCaptionLabel);
        Controls.Add(currentMicValueLabel);
        Controls.Add(currentMicCaptionLabel);
        Controls.Add(titleLabel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "GoMicFuckYourself";
        ((System.ComponentModel.ISupportInitialize)volumeNumericUpDown).EndInit();
        trayContextMenuStrip.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
