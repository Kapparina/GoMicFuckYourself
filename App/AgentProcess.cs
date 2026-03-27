using System.Diagnostics;

namespace GoMicFuckYourself.Tray;

internal static class AgentProcess
{
    public static void TryStartInstalledAgent()
    {
        try
        {
            var agentPath = ResolveInstalledAgentPath();
            if (agentPath is null || !File.Exists(agentPath))
            {
                return;
            }

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = agentPath,
                WorkingDirectory = Path.GetDirectoryName(agentPath) ?? AppContext.BaseDirectory,
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    public static string? ResolveInstalledAgentPath()
    {
        var trayDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var installRoot = Directory.GetParent(trayDirectory)?.FullName;
        if (string.IsNullOrWhiteSpace(installRoot))
        {
            return null;
        }

        return Path.Combine(installRoot, "Agent", "GoMicFuckYourself.Agent.exe");
    }
}
