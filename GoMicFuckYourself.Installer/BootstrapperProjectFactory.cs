using WixSharp;
using WixSharp.Bootstrapper;

namespace GoMicFuckYourself.Installer;

internal static class BootstrapperProjectFactory
{
    private const string DotNetDesktopRuntimeCheckProperty = "DOTNET_DESKTOP_RUNTIME_FOUND";

    public static Bundle Create(string msiPath, string dotNetDesktopRuntimeInstallerPath, string version)
    {
        var bundle = new Bundle(
            InstallerConstants.BootstrapperName,
            new ExePackage(dotNetDesktopRuntimeInstallerPath)
            {
                Name = Path.GetFileName(dotNetDesktopRuntimeInstallerPath),
                DisplayName = $".NET Desktop Runtime {InstallerConstants.DotNetDesktopRuntimeVersion} (x64)",
                PerMachine = true,
                Permanent = true,
                DetectCondition = DotNetDesktopRuntimeCheckProperty,
                InstallArguments = "/install /quiet /norestart"
            },
            new MsiPackage(msiPath));

        bundle.Version = new Version(version);
        bundle.UpgradeCode = new Guid(InstallerConstants.BootstrapperUpgradeCode);
        bundle.AboutUrl = InstallerConstants.SupportContact;
        bundle.DisableModify = "yes";
        bundle.DisableRemove = false;
        bundle.OutDir = Path.GetDirectoryName(msiPath) ?? AppContext.BaseDirectory;
        bundle.OutFileName = $"{InstallerConstants.ProductName}-{version}-bootstrapper";

        bundle.GenericItems.Add(new DotNetCompatibilityCheck(
            DotNetDesktopRuntimeCheckProperty,
            RollForward.latestPatch,
            RuntimeType.desktop,
            Platform.x64,
            new Version(10, 0, 0, 0)));

        return bundle;
    }
}
