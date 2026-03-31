namespace GoMicFuckYourself.Agent.Logging;

internal sealed class AgentLifecycleHostedService : IHostedService
{
    private readonly ILogger<AgentLifecycleHostedService> _logger;

    public AgentLifecycleHostedService(ILogger<AgentLifecycleHostedService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agent startup completed.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agent shutdown requested.");
        return Task.CompletedTask;
    }
}