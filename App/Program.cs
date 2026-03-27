namespace GoMicFuckYourself.Tray;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        AgentProcess.TryStartInstalledAgent();

        var firstRun = args.Any(arg => string.Equals(arg, "--first-run", StringComparison.OrdinalIgnoreCase));
        using var pipeClient = new ServicePipeClient();
        Application.Run(new MainForm(pipeClient, firstRun));
    }
}
