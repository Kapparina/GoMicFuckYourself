using System.Diagnostics;

namespace GoMicFuckYourself.Tray;

internal static class ProcessCoordinator
{
    private const string TrayProcessName = "GoMicFuckYourself.Tray";
    private const string AgentProcessName = "GoMicFuckYourself.Agent";

    public static void TerminateOtherTrayInstances()
    {
        TerminateProcesses(TrayProcessName, Process.GetCurrentProcess().Id);
    }

    public static void TerminateAgentInstances()
    {
        TerminateProcesses(AgentProcessName, null);
    }

    public static void RestartAgent()
    {
        TerminateAgentInstances();
        AgentProcess.TryStartInstalledAgent();
    }

    private static void TerminateProcesses(string processName, int? currentProcessIdToSkip)
    {
        foreach (var process in Process.GetProcessesByName(processName))
            try
            {
                if (currentProcessIdToSkip.HasValue && process.Id == currentProcessIdToSkip.Value) continue;

                process.Kill(true);
                process.WaitForExit(5000);
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
    }
}