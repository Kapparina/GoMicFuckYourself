namespace GoMicFuckYourself.Installer;

internal static class InstallerConstants
{
    public const string ProductName = "GoMicFuckYourself";
    public const string BootstrapperName = "GoMicFuckYourself Setup";
    public const string Manufacturer = "GoMicFuckYourself";
    public const string ProductDescription = "Enforces a selected microphone as the default capture and communications device and re-applies the configured input volume.";
    public const string SupportContact = "https://github.com/Kapparina/GoMicFuckYourself/releases";
    public const string Comments = "Installs the GoMicFuckYourself agent, tray app, and shared machine configuration.";
    public const string DefaultVersion = "0.1.0";
    public const string UpgradeCode = "7E383A7A-9580-48A6-818E-B173FEE980C8";
    public const string BootstrapperUpgradeCode = "4D2692E6-C147-43F4-BF64-A9BE790F6A5F";
    public const string DotNetDesktopRuntimeVersion = "10.0.3";
    public const string DotNetDesktopRuntimeDownloadPage = "https://dotnet.microsoft.com/en-US/download/dotnet/10.0";

    public const string LaunchOnExitCheckboxText = "Launch GoMicFuckYourself setup";

    public static readonly string InstallRoot = @"%ProgramFiles%\GoMicFuckYourself";
    public static readonly string ProgramDataRoot = @"%CommonAppData%\GoMicFuckYourself";
}
