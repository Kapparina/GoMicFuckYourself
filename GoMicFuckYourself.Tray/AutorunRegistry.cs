using Microsoft.Win32;

namespace GoMicFuckYourself.Tray;

internal static class AutorunRegistry
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string TrayAutorunName = "GoMicFuckYourself.Tray";

    public static bool IsEnabledForCurrentUser()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        if (key is null)
        {
            return false;
        }

        return key.GetValue(TrayAutorunName) is string trayValue &&
               !string.IsNullOrWhiteSpace(trayValue);
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

        key.SetValue(TrayAutorunName, Quote(trayPath));
    }

    public static void DisableForCurrentUser()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                        ?? throw new InvalidOperationException("The current-user Run registry key could not be opened.");

        key.DeleteValue(TrayAutorunName, throwOnMissingValue: false);
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
