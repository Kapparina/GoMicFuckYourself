using WixSharp;
using File = WixSharp.File;

namespace GoMicFuckYourself.Installer;

internal static class InstallerProjectFactory
{
    public static ManagedProject Create(PayloadLayout payload)
    {
        var serviceFile = new File(new Id("SERVICE_EXE"), payload.ServiceExecutablePath)
        {
            ServiceInstaller = new ServiceInstaller
            {
                Name = InstallerConstants.ServiceName,
                DisplayName = InstallerConstants.ServiceDisplayName,
                Description = InstallerConstants.ServiceDescription,
                StartOn = SvcEvent.Install,
                StopOn = SvcEvent.InstallUninstall_Wait,
                RemoveOn = SvcEvent.Uninstall_Wait
            }
        };

        var project = new ManagedProject(
            InstallerConstants.ProductName,
            new Dir(
                new Id("INSTALLDIR"),
                InstallerConstants.InstallRoot,
                new Dir("Service", BuildPayloadDirectory(payload.ServicePayloadRoot, serviceFile)),
                new Dir("Tray", BuildPayloadDirectory(payload.TrayPayloadRoot))),
            new Dir(
                new Id("PROGRAMDATADIR"),
                InstallerConstants.ProgramDataRoot,
                new File(payload.DefaultConfigPath)),
            new RegValue(
                WixSharp.RegistryHive.LocalMachine,
                @"Software\Microsoft\Windows\CurrentVersion\Run",
                InstallerConstants.TrayAutorunName,
                "[INSTALLDIR]Tray\\GoMicFuckYourself.Tray.exe --first-run"));

        project.GUID = new Guid(InstallerConstants.UpgradeCode);
        project.Version = new Version(InstallerConstants.Version);
        project.OutFileName = $"{InstallerConstants.ProductName}-{InstallerConstants.Version}";
        project.OutDir = Path.Combine(payload.OutputRoot, "msi");
        project.Scope = InstallScope.perMachine;
        project.Platform = Platform.x64;
        project.ControlPanelInfo.Manufacturer = InstallerConstants.Manufacturer;
        project.ControlPanelInfo.Comments = InstallerConstants.Comments;
        project.ControlPanelInfo.Contact = InstallerConstants.SupportContact;
        project.ControlPanelInfo.HelpLink = "https://github.com";
        project.ControlPanelInfo.InstallLocation = "[INSTALLDIR]";
        project.ControlPanelInfo.NoModify = true;
        project.ControlPanelInfo.NoRepair = false;
        project.ControlPanelInfo.Readme = "[INSTALLDIR]Tray\\GoMicFuckYourself.Tray.exe";
        project.Description = InstallerConstants.ProductDescription;
        project.ControlPanelInfo.ProductIcon = payload.TrayExecutablePath;
        project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;

        return project;
    }

    private static WixEntity[] BuildPayloadDirectory(string sourceDirectory, params File[] pinnedFiles)
    {
        var entities = new List<WixEntity>();
        var pinnedPaths = new HashSet<string>(
            pinnedFiles.Select(file => Path.GetFullPath(file.Name)),
            StringComparer.OrdinalIgnoreCase);

        entities.AddRange(pinnedFiles);

        foreach (var filePath in Directory.GetFiles(sourceDirectory))
        {
            var fullPath = Path.GetFullPath(filePath);
            if (pinnedPaths.Contains(fullPath))
            {
                continue;
            }

            entities.Add(new File(filePath));
        }

        return entities.ToArray();
    }
}
