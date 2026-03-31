using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Win32;
using WixToolset.Dtf.WindowsInstaller;

namespace GoMicFuckYourself.Installer;

public static class UninstallCleanupActions
{
    [CustomAction]
    public static ActionResult EnsureDefaultConfig(Session session)
    {
        try
        {
            var configPath = GetConfigPath();
            var configDirectory = Path.GetDirectoryName(configPath);
            if (string.IsNullOrWhiteSpace(configDirectory)) return ActionResult.Success;

            Directory.CreateDirectory(configDirectory);

            if (File.Exists(configPath))
            {
                if (IsConfigCompatible(configPath))
                {
                    session.Log($"Preserving existing config at '{configPath}'.");
                    return ActionResult.Success;
                }

                session.Log(
                    $"Existing config at '{configPath}' was incompatible. Replacing it with the default config.");
            }

            File.WriteAllText(configPath, CreateDefaultConfigJson(), Encoding.UTF8);
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            session.Log($"Failed to seed default config: {exception}");
            return ActionResult.Success;
        }
    }

    [CustomAction]
    public static ActionResult EnsureAgentEventSource(Session session)
    {
        try
        {
            if (!EventLog.SourceExists(InstallerConstants.AgentEventSourceName))
                EventLog.CreateEventSource(
                    new EventSourceCreationData(
                        InstallerConstants.AgentEventSourceName,
                        InstallerConstants.AgentEventLogName));

            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            session.Log($"Failed to create agent event source: {exception}");
            return ActionResult.Success;
        }
    }

    [CustomAction]
    public static ActionResult RemoveCurrentUserAutorun(Session session)
    {
        const string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string startupApprovedRunKeyPath =
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        const string trayAutorunName = "GoMicFuckYourself.Tray";

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(runKeyPath, true);
            using var startupApprovedKey = Registry.CurrentUser.CreateSubKey(startupApprovedRunKeyPath, true);
            key?.DeleteValue(trayAutorunName, false);
            startupApprovedKey?.DeleteValue(trayAutorunName, false);
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            session.Log($"Failed to remove current-user autorun entry: {exception}");
            return ActionResult.Success;
        }
    }

    [CustomAction]
    public static ActionResult RemoveAgentEventSource(Session session)
    {
        try
        {
            if (EventLog.SourceExists(InstallerConstants.AgentEventSourceName))
                EventLog.DeleteEventSource(InstallerConstants.AgentEventSourceName);

            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            session.Log($"Failed to remove agent event source: {exception}");
            return ActionResult.Success;
        }
    }

    [CustomAction]
    public static ActionResult RemoveUserConfig(Session session)
    {
        try
        {
            var configPath = GetConfigPath();
            var configDirectory = Path.GetDirectoryName(configPath);

            if (File.Exists(configPath)) File.Delete(configPath);

            if (!string.IsNullOrWhiteSpace(configDirectory) &&
                Directory.Exists(configDirectory) &&
                !Directory.EnumerateFileSystemEntries(configDirectory).Any())
                Directory.Delete(configDirectory);

            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            session.Log($"Failed to remove user config: {exception}");
            return ActionResult.Success;
        }
    }

    private static string GetConfigPath()
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return Path.Combine(programDataPath, "GoMicFuckYourself", "agent-config.json");
    }

    private static bool IsConfigCompatible(string configPath)
    {
        try
        {
            using var stream = File.OpenRead(configPath);
            var serializer = new DataContractJsonSerializer(typeof(InstallerServiceConfig));
            var config = serializer.ReadObject(stream) as InstallerServiceConfig;
            return config is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string CreateDefaultConfigJson()
    {
        var serializer = new DataContractJsonSerializer(typeof(InstallerServiceConfig));
        using var stream = new MemoryStream();
        serializer.WriteObject(stream, new InstallerServiceConfig
        {
            TargetVolumePercent = 100f,
            EnforcementEnabled = true
        });

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [DataContract]
    private sealed class InstallerServiceConfig
    {
        [DataMember(Name = "selectedCaptureDeviceId")]
        public string? SelectedCaptureDeviceId { get; set; }

        [DataMember(Name = "targetVolumePercent")]
        public float? TargetVolumePercent { get; set; }

        [DataMember(Name = "enforcementEnabled")]
        public bool EnforcementEnabled { get; set; } = true;
    }
}