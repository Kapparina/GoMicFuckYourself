using GoMicFuckYourself.Contracts.Configuration;

namespace GoMicFuckYourself.Service.Configuration;

public interface IConfigStore
{
    Task<ServiceConfig> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(ServiceConfig config, CancellationToken cancellationToken);
}
