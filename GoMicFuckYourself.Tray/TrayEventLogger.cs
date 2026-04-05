using System.Diagnostics;

namespace GoMicFuckYourself.Tray;

internal static class TrayEventLogger
{
    private const string AgentEventSourceName = "GoMicFuckYourself.Agent";

    public static void LogStartup(bool firstRunRequested, bool firstRunDetected)
    {
        try
        {
            if (!EventLog.SourceExists(AgentEventSourceName)) return;

            EventLog.WriteEntry(
                AgentEventSourceName,
                $"Tray application startup requested. FirstRunRequested={firstRunRequested}; FirstRunDetected={firstRunDetected}.",
                EventLogEntryType.Information);
        }
        catch
        {
        }
    }

    public static void LogTrayRequestedTermination()
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
}
