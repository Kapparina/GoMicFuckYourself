using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GoMicFuckYourself.Service.Enforcement;

public sealed class MicPolicyBackgroundService : BackgroundService
{
    private static readonly TimeSpan EnforcementInterval = TimeSpan.FromSeconds(10);

    private readonly IMicPolicyEngine _micPolicyEngine;
    private readonly ILogger<MicPolicyBackgroundService> _logger;

    public MicPolicyBackgroundService(
        IMicPolicyEngine micPolicyEngine,
        ILogger<MicPolicyBackgroundService> logger)
    {
        _micPolicyEngine = micPolicyEngine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _micPolicyEngine.StartAsync(stoppingToken);

        using var timer = new PeriodicTimer(EnforcementInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _micPolicyEngine.PeriodicEnforceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Periodic microphone enforcement failed.");
            }
        }
    }
}
