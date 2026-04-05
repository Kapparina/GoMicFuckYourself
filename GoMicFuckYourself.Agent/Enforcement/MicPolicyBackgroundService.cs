namespace GoMicFuckYourself.Agent.Enforcement;

public sealed class MicPolicyBackgroundService : BackgroundService
{
    private readonly IMicPolicyEngine _micPolicyEngine;

    public MicPolicyBackgroundService(IMicPolicyEngine micPolicyEngine)
    {
        _micPolicyEngine = micPolicyEngine;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _micPolicyEngine.StartAsync(stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
