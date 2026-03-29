using Microsoft.Win32;

namespace GoMicFuckYourself.Tray;

internal static class AutorunRegistry
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupApprovedRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string TrayAutorunName = "GoMicFuckYourself.Tray";
    private static readonly byte[] EnabledStartupValue = [0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    private static readonly byte[] DisabledStartupValue = [0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    public static bool IsEnabledForCurrentUser()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        if (runKey?.GetValue(TrayAutorunName) is not string trayValue ||
            string.IsNullOrWhiteSpace(trayValue))
        {
            return false;
        }

        using var startupApprovedKey = Registry.CurrentUser.OpenSubKey(StartupApprovedRunKeyPath, writable: false);
        if (startupApprovedKey?.GetValue(TrayAutorunName) is not byte[] startupValue || startupValue.Length == 0)
        {
            return true;
        }

        return startupValue[0] != 0x03;
    }

    public static void EnableForCurrentUser()
    {
        var trayPath = ResolveInstalledTrayPath();

        if (string.IsNullOrWhiteSpace(trayPath))
        {
            throw new InvalidOperationException("Installed tray path could not be resolved.");
        }

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                        ?? throw new InvalidOperationException("The current-user Run registry key could not be opened.");
        using var startupApprovedKey = Registry.CurrentUser.CreateSubKey(StartupApprovedRunKeyPath, writable: true)
                                       ?? throw new InvalidOperationException("The current-user StartupApproved Run registry key could not be opened.");

        key.SetValue(TrayAutorunName, Quote(trayPath));
        startupApprovedKey.SetValue(TrayAutorunName, EnabledStartupValue, RegistryValueKind.Binary);
    }

    public static void DisableForCurrentUser()
    {
        var trayPath = ResolveInstalledTrayPath();
        if (string.IsNullOrWhiteSpace(trayPath))
        {
            throw new InvalidOperationException("Installed tray path could not be resolved.");
        }

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                        ?? throw new InvalidOperationException("The current-user Run registry key could not be opened.");
        using var startupApprovedKey = Registry.CurrentUser.CreateSubKey(StartupApprovedRunKeyPath, writable: true)
                                       ?? throw new InvalidOperationException("The current-user StartupApproved Run registry key could not be opened.");

        key.SetValue(TrayAutorunName, Quote(trayPath));
        startupApprovedKey.SetValue(TrayAutorunName, DisabledStartupValue, RegistryValueKind.Binary);
    }

    public static void SetForCurrentUser(bool enabled)
    {
        if (enabled)
        {
            EnableForCurrentUser();
        }
        else
        {
            DisableForCurrentUser();
        }
    }

    private static string ResolveInstalledTrayPath()
    {
        var trayDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.Combine(trayDirectory, "GoMicFuckYourself.Tray.exe");
    }

    private static string Quote(string path) => $"\"{path}\"";
}
