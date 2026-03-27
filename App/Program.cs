namespace GoMicFuckYourself.Tray;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var firstRunRequested = args.Any(arg => string.Equals(arg, "--first-run", StringComparison.OrdinalIgnoreCase));
        var firstRun = firstRunRequested || SetupStateDetector.IsSetupPending();

        ApplicationConfiguration.Initialize();

        if (firstRun)
        {
            ProcessCoordinator.TerminateOtherTrayInstances();
            ProcessCoordinator.TerminateAgentInstances();

            if (!TrayInstance.TryAcquire(out var firstRunMutex))
            {
                MessageBox.Show(
                    "GoMicFuckYourself setup is already running.",
                    "GoMicFuckYourself",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var _ = firstRunMutex;
            using var firstRunPipeClient = new AgentPipeClient();
            Application.Run(new MainForm(firstRunPipeClient, firstRun: true));
            return;
        }

        if (!TrayInstance.TryAcquire(out var trayMutex))
        {
            MessageBox.Show(
                "GoMicFuckYourself is already running in the system tray.",
                "GoMicFuckYourself",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var __ = trayMutex;
        using var normalPipeClient = new AgentPipeClient();
        Application.Run(new MainForm(normalPipeClient, firstRun: false));
    }
}
