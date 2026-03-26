namespace GoMicFuckYourself.Installer;

internal static class InstallerConstants
{
    public const string ProductName = "GoMicFuckYourself";
    public const string Manufacturer = "GoMicFuckYourself";
    public const string ProductDescription = "Enforces a selected microphone as the default capture and communications device and re-applies the configured input volume.";
    public const string SupportContact = "GitHub Releases";
    public const string Comments = "Installs the GoMicFuckYourself service, tray app, and shared machine configuration.";
    public const string Version = "0.1.0";
    public const string UpgradeCode = "7E383A7A-9580-48A6-818E-B173FEE980C8";

    public const string ServiceName = "GoMicFuckYourself.Service";
    public const string ServiceDisplayName = "GoMicFuckYourself Service";
    public const string ServiceDescription = "Enforces the configured microphone as the default capture and communications device.";

    public const string TrayAutorunName = "GoMicFuckYourself.Tray";

    public static readonly string InstallRoot = @"%ProgramFiles%\GoMicFuckYourself";
    public static readonly string ProgramDataRoot = @"%CommonAppData%\GoMicFuckYourself";
}
