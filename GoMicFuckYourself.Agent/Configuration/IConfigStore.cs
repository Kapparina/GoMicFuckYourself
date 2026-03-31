using GoMicFuckYourself.Contracts.Configuration;

namespace GoMicFuckYourself.Agent.Configuration;

public interface IConfigStore
{
    Task<ServiceConfig> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(ServiceConfig config, CancellationToken cancellationToken);
}