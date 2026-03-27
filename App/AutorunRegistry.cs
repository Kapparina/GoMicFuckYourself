using Microsoft.Win32;

namespace GoMicFuckYourself.Tray;

internal static class AutorunRegistry
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AgentAutorunName = "GoMicFuckYourself.Agent";
    private const string TrayAutorunName = "GoMicFuckYourself.Tray";

    public static void EnableForCurrentUser()
    {
        var trayPath = ResolveInstalledTrayPath();
        var agentPath = AgentProcess.ResolveInstalledAgentPath();

        if (string.IsNullOrWhiteSpace(trayPath) || string.IsNullOrWhiteSpace(agentPath))
        {
            throw new InvalidOperationException("Installed tray or agent path could not be resolved.");
        }

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                        ?? throw new InvalidOperationException("The current-user Run registry key could not be opened.");

        key.SetValue(TrayAutorunName, Quote(trayPath));
        key.SetValue(AgentAutorunName, Quote(agentPath));
    }

    private static string ResolveInstalledTrayPath()
    {
        var trayDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.Combine(trayDirectory, "GoMicFuckYourself.Tray.exe");
    }

    private static string Quote(string path) => $"\"{path}\"";
}
