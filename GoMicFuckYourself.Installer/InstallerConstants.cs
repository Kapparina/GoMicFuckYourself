namespace GoMicFuckYourself.Installer;

internal static class InstallerConstants
{
    public const string ProductName = "GoMicFuckYourself";
    public const string ReleasesUrl = "https://github.com/Kapparina/GoMicFuckYourself/releases";
    public const string Manufacturer = "github.com/Kapparina";
    public const string ProductDescription = "Enforces a selected microphone as the default capture and communications device and re-applies the configured input volume.";
    public const string SupportContact = "GitHub Releases";
    public const string Comments = "Installs the GoMicFuckYourself agent, tray app, and shared machine configuration. Release notes and changelogs are published on the GitHub Releases page.";
    public const string DefaultVersion = "dev";
    public const string UpgradeCode = "7E383A7A-9580-48A6-818E-B173FEE980C8";

    public const string LaunchOnExitCheckboxText = "Launch GoMicFuckYourself setup";
    public const string AgentEventLogName = "Application";
    public const string AgentEventSourceName = "GoMicFuckYourself.Agent";

    public static readonly string InstallRoot = @"%ProgramFiles%\GoMicFuckYourself";
    public static readonly string ProgramDataRoot = @"%CommonAppData%\GoMicFuckYourself";
}
