using System.Diagnostics;
using System.IO.Pipes;
using GoMicFuckYourself.Contracts.Ipc;

namespace GoMicFuckYourself.Tray;

internal static class AgentProcess
{
    public static bool TryStartInstalledAgent()
    {
        try
        {
            var agentPath = ResolveInstalledAgentPath();
            if (agentPath is null || !File.Exists(agentPath))
            {
                return false;
            }

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = agentPath,
                WorkingDirectory = Path.GetDirectoryName(agentPath) ?? AppContext.BaseDirectory,
                UseShellExecute = true
            });
            return process is not null;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> EnsureAgentReadyAsync(CancellationToken cancellationToken, bool startIfNeeded)
    {
        if (await IsAgentReachableAsync(cancellationToken))
        {
            return true;
        }

        if (!startIfNeeded || !TryStartInstalledAgent())
        {
            return false;
        }

        var startedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - startedAt < TimeSpan.FromSeconds(12))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await IsAgentReachableAsync(cancellationToken))
            {
                return true;
            }

            await Task.Delay(400, cancellationToken);
        }

        return false;
    }

    private static async Task<bool> IsAgentReachableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeConstants.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(500, cancellationToken);
            return pipe.IsConnected;
        }
        catch
        {
            return false;
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
