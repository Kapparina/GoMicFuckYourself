using WixSharp;
using File = WixSharp.File;

namespace GoMicFuckYourself.Installer;

internal static class InstallerProjectFactory
{
    public static ManagedProject Create(PayloadLayout payload, string version)
    {
        var displayName = $"{InstallerConstants.ProductName} {version}";
        var agentFile = new File(new Id("AGENT_EXE"), payload.AgentExecutablePath);
        var trayFile = new File(new Id("TRAY_EXE"), payload.TrayExecutablePath);

        var project = new ManagedProject(
            displayName,
            new Dir(
                new Id("INSTALLDIR"),
                InstallerConstants.InstallRoot,
                new Dir("Agent", BuildPayloadDirectory(payload.AgentPayloadRoot, agentFile)),
                new Dir("Tray", BuildPayloadDirectory(payload.TrayPayloadRoot, trayFile))),
            new Dir(
                @"%ProgramMenu%\GoMicFuckYourself",
                new ExeFileShortcut(InstallerConstants.ProductName, "[#TRAY_EXE]", "")
                {
                    WorkingDirectory = "INSTALLDIR"
                }),
            new Dir(
                new Id("PROGRAMDATADIR"),
                InstallerConstants.ProgramDataRoot,
                new DirPermission("Users", GenericPermission.Read | GenericPermission.Write)),
            new LaunchApplicationFromExitDialog("TRAY_EXE", InstallerConstants.LaunchOnExitCheckboxText))
        {
            GUID = new Guid(InstallerConstants.UpgradeCode),
            Version = new Version(version),
            OutFileName = $"{InstallerConstants.ProductName}-{version}",
            OutDir = Path.Combine(payload.OutputRoot, "msi"),
            Scope = InstallScope.perMachine,
            Platform = Platform.x64,
            ControlPanelInfo =
            {
                Manufacturer = InstallerConstants.Manufacturer,
                Comments = $"{InstallerConstants.Comments} Installed version: {version}.",
                Contact = InstallerConstants.SupportContact,
                HelpLink = InstallerConstants.ReleasesUrl,
                InstallLocation = "[INSTALLDIR]",
                NoModify = true,
                NoRepair = false,
                ProductIcon = payload.TrayExecutablePath
            },
            Description =
                $"{InstallerConstants.ProductDescription} Installing version {version}. Changelog: {InstallerConstants.ReleasesUrl}",
            MajorUpgradeStrategy = MajorUpgradeStrategy.Default,
            LicenceFile = Path.Combine(AppContext.BaseDirectory, "Assets", "Licence.rtf"),
            Actions =
            [
                new ManagedAction(
                    UninstallCleanupActions.EnsureAgentEventSource,
                    Return.ignore,
                    When.After,
                    Step.InstallFinalize,
                    new Condition("NOT Installed")),
                new ManagedAction(
                    UninstallCleanupActions.EnsureDefaultConfig,
                    Return.ignore,
                    When.After,
                    Step.InstallFinalize,
                    Condition.NOT_Installed),
                new ManagedAction(
                    UninstallCleanupActions.RemoveCurrentUserAutorun,
                    Return.ignore,
                    When.Before,
                    Step.RemoveFiles,
                    new Condition("REMOVE=\"ALL\" AND NOT UPGRADINGPRODUCTCODE")),
                new ManagedAction(
                    UninstallCleanupActions.RemoveAgentEventSource,
                    Return.ignore,
                    When.Before,
                    Step.RemoveFiles,
                    new Condition("REMOVE=\"ALL\" AND NOT UPGRADINGPRODUCTCODE")),
                new ManagedAction(
                    UninstallCleanupActions.RemoveUserConfig,
                    Return.ignore,
                    When.Before,
                    Step.RemoveFiles,
                    new Condition("REMOVE=\"ALL\" AND NOT UPGRADINGPRODUCTCODE"))
            ]
        };

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
