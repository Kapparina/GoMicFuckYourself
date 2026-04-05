using System.Diagnostics;

namespace GoMicFuckYourself.Tray;

internal static class ProcessCoordinator
{
    private const string TrayProcessName = "GoMicFuckYourself.Tray";
    private const string AgentProcessName = "GoMicFuckYourself.Agent";
    private const string AgentEventSourceName = "GoMicFuckYourself.Agent";

    public static void TerminateOtherTrayInstances()
    {
        TerminateProcesses(TrayProcessName, Process.GetCurrentProcess().Id);
    }

    public static void TerminateAgentInstances(bool logTrayRequestedTermination = false)
    {
        if (logTrayRequestedTermination) LogTrayRequestedTermination();
        TerminateProcesses(AgentProcessName, null);
    }

    public static void RestartAgent()
    {
        TerminateAgentInstances();
        AgentProcess.TryStartInstalledAgent();
    }

    private static void LogTrayRequestedTermination()
    {
        try
        {
            if (!EventLog.SourceExists(AgentEventSourceName)) return;

            EventLog.WriteEntry(
                AgentEventSourceName,
                "Agent termination was requested from the tray application exit command.",
                EventLogEntryType.Information);
        }
        catch
        {
        }
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
