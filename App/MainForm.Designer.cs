namespace GoMicFuckYourself.Tray;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private NotifyIcon trayNotifyIcon;
    private ContextMenuStrip trayContextMenuStrip;
    private ToolStripMenuItem openTrayMenuItem;
    private ToolStripMenuItem refreshTrayMenuItem;
    private ToolStripSeparator traySeparator;
    private ToolStripMenuItem exitTrayMenuItem;
    private Label titleLabel;
    private Label deviceLabel;
    private ComboBox devicesComboBox;
    private Label volumeLabel;
    private NumericUpDown volumeNumericUpDown;
    private CheckBox enforcementEnabledCheckBox;
    private Button refreshButton;
    private Button saveButton;
    private Button applyButton;
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
        refreshTrayMenuItem = new ToolStripMenuItem();
        traySeparator = new ToolStripSeparator();
        exitTrayMenuItem = new ToolStripMenuItem();
        trayNotifyIcon = new NotifyIcon(components);
        titleLabel = new Label();
        deviceLabel = new Label();
        devicesComboBox = new ComboBox();
        volumeLabel = new Label();
        volumeNumericUpDown = new NumericUpDown();
        enforcementEnabledCheckBox = new CheckBox();
        refreshButton = new Button();
        saveButton = new Button();
        applyButton = new Button();
        statusLabel = new Label();
        errorLabel = new Label();
        ((System.ComponentModel.ISupportInitialize)volumeNumericUpDown).BeginInit();
        trayContextMenuStrip.SuspendLayout();
        SuspendLayout();
        // 
        // trayContextMenuStrip
        // 
        trayContextMenuStrip.Items.AddRange(new ToolStripItem[] { openTrayMenuItem, refreshTrayMenuItem, traySeparator, exitTrayMenuItem });
        trayContextMenuStrip.Name = "trayContextMenuStrip";
        trayContextMenuStrip.Size = new Size(114, 76);
        // 
        // openTrayMenuItem
        // 
        openTrayMenuItem.Name = "openTrayMenuItem";
        openTrayMenuItem.Size = new Size(113, 22);
        openTrayMenuItem.Text = "Open";
        openTrayMenuItem.Click += openTrayMenuItem_Click;
        // 
        // refreshTrayMenuItem
        // 
        refreshTrayMenuItem.Name = "refreshTrayMenuItem";
        refreshTrayMenuItem.Size = new Size(113, 22);
        refreshTrayMenuItem.Text = "Refresh";
        refreshTrayMenuItem.Click += refreshTrayMenuItem_Click;
        // 
        // traySeparator
        // 
        traySeparator.Name = "traySeparator";
        traySeparator.Size = new Size(110, 6);
        // 
        // exitTrayMenuItem
        // 
        exitTrayMenuItem.Name = "exitTrayMenuItem";
        exitTrayMenuItem.Size = new Size(113, 22);
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
        deviceLabel.AutoSize = true;
        deviceLabel.Location = new Point(24, 75);
        deviceLabel.Name = "deviceLabel";
        deviceLabel.Size = new Size(116, 15);
        deviceLabel.TabIndex = 1;
        deviceLabel.Text = "Capture microphone";
        devicesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        devicesComboBox.FormattingEnabled = true;
        devicesComboBox.Location = new Point(24, 93);
        devicesComboBox.Name = "devicesComboBox";
        devicesComboBox.Size = new Size(520, 23);
        devicesComboBox.TabIndex = 2;
        volumeLabel.AutoSize = true;
        volumeLabel.Location = new Point(24, 137);
        volumeLabel.Name = "volumeLabel";
        volumeLabel.Size = new Size(80, 15);
        volumeLabel.TabIndex = 3;
        volumeLabel.Text = "Target volume";
        volumeNumericUpDown.Location = new Point(24, 155);
        volumeNumericUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        volumeNumericUpDown.Name = "volumeNumericUpDown";
        volumeNumericUpDown.Size = new Size(120, 23);
        volumeNumericUpDown.TabIndex = 4;
        volumeNumericUpDown.Value = new decimal(new int[] { 100, 0, 0, 0 });
        enforcementEnabledCheckBox.AutoSize = true;
        enforcementEnabledCheckBox.Checked = true;
        enforcementEnabledCheckBox.CheckState = CheckState.Checked;
        enforcementEnabledCheckBox.Location = new Point(24, 198);
        enforcementEnabledCheckBox.Name = "enforcementEnabledCheckBox";
        enforcementEnabledCheckBox.Size = new Size(192, 19);
        enforcementEnabledCheckBox.TabIndex = 5;
        enforcementEnabledCheckBox.Text = "Enable continuous enforcement";
        enforcementEnabledCheckBox.UseVisualStyleBackColor = true;
        refreshButton.Location = new Point(24, 244);
        refreshButton.Name = "refreshButton";
        refreshButton.Size = new Size(90, 32);
        refreshButton.TabIndex = 6;
        refreshButton.Text = "Refresh";
        refreshButton.UseVisualStyleBackColor = true;
        refreshButton.Click += refreshButton_Click;
        saveButton.Location = new Point(120, 244);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(90, 32);
        saveButton.TabIndex = 7;
        saveButton.Text = "Save";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += saveButton_Click;
        applyButton.Location = new Point(216, 244);
        applyButton.Name = "applyButton";
        applyButton.Size = new Size(140, 32);
        applyButton.TabIndex = 8;
        applyButton.Text = "Save and enforce";
        applyButton.UseVisualStyleBackColor = true;
        applyButton.Click += applyButton_Click;
        statusLabel.Location = new Point(24, 296);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(520, 42);
        statusLabel.TabIndex = 9;
        statusLabel.Text = "Loading agent state...";
        errorLabel.ForeColor = Color.Firebrick;
        errorLabel.Location = new Point(24, 340);
        errorLabel.Name = "errorLabel";
        errorLabel.Size = new Size(520, 48);
        errorLabel.TabIndex = 10;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(584, 411);
        Controls.Add(errorLabel);
        Controls.Add(statusLabel);
        Controls.Add(applyButton);
        Controls.Add(saveButton);
        Controls.Add(refreshButton);
        Controls.Add(enforcementEnabledCheckBox);
        Controls.Add(volumeNumericUpDown);
        Controls.Add(volumeLabel);
        Controls.Add(devicesComboBox);
        Controls.Add(deviceLabel);
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
