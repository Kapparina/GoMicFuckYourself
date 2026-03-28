using WixSharp;
using File = WixSharp.File;

namespace GoMicFuckYourself.Installer;

internal static class InstallerProjectFactory
{
    public static ManagedProject Create(PayloadLayout payload)
    {
        var agentFile = new File(new Id("AGENT_EXE"), payload.AgentExecutablePath);
        var trayFile = new File(new Id("TRAY_EXE"), payload.TrayExecutablePath);

        var project = new ManagedProject(
            InstallerConstants.ProductName,
            new Dir(
                new Id("INSTALLDIR"),
                InstallerConstants.InstallRoot,
                new Dir("Agent", BuildPayloadDirectory(payload.AgentPayloadRoot, agentFile)),
                new Dir("Tray", BuildPayloadDirectory(payload.TrayPayloadRoot, trayFile))),
            new Dir(
                new Id("PROGRAMDATADIR"),
                InstallerConstants.ProgramDataRoot,
                new File(payload.DefaultConfigPath)),
            new LaunchApplicationFromExitDialog("TRAY_EXE", InstallerConstants.LaunchOnExitCheckboxText));

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
        project.Description = InstallerConstants.ProductDescription;
        project.ControlPanelInfo.ProductIcon = payload.TrayExecutablePath;
        project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;
        project.LicenceFile = Path.Combine(AppContext.BaseDirectory, "Assets", "License.rtf");

        return project;
    }

    private static WixEntity[] BuildPayloadDirectory(string sourceDirectory, params File[] pinnedFiles)
    {
        var entities = new List<WixEntity>();
        var pinnedPaths = new HashSet<string>(
            pinnedFiles.Select(file => Path.GetFullPath(file.Name)),
            StringComparer.OrdinalIgnoreCase);

        entities.AddRange(pinnedFiles);

        AddDirectoryContents(entities, sourceDirectory, pinnedPaths);

        return entities.ToArray();
    }

    private static void AddDirectoryContents(
        ICollection<WixEntity> entities,
        string sourceDirectory,
        ISet<string> pinnedPaths)
    {
        foreach (var filePath in Directory.GetFiles(sourceDirectory))
        {
            var fullPath = Path.GetFullPath(filePath);
            if (pinnedPaths.Contains(fullPath))
            {
                continue;
            }

            entities.Add(new File(filePath));
        }

        foreach (var directoryPath in Directory.GetDirectories(sourceDirectory))
        {
            var childEntities = new List<WixEntity>();
            AddDirectoryContents(childEntities, directoryPath, pinnedPaths);

            entities.Add(new Dir(Path.GetFileName(directoryPath), childEntities.ToArray()));
        }
    }
}
