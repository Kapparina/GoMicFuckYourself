using GoMicFuckYourself.Contracts.Configuration;
using GoMicFuckYourself.Contracts.Enforcement;

namespace GoMicFuckYourself.Agent.Enforcement;

public interface IMicPolicyEngine
{
    MicEnforcementStatus GetStatus();

    Task<ServiceConfig> GetConfigAsync(CancellationToken cancellationToken);

    Task SaveConfigAsync(ServiceConfig config, CancellationToken cancellationToken);

    Task StartAsync(CancellationToken cancellationToken);

    Task ForceEnforceAsync(CancellationToken cancellationToken);

    Task PeriodicEnforceAsync(CancellationToken cancellationToken);
}
