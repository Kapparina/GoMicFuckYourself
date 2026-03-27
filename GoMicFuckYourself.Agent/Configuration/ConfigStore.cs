using System.Text.Json;
using GoMicFuckYourself.Contracts.Configuration;
using Microsoft.Extensions.Logging;

namespace GoMicFuckYourself.Agent.Configuration;

public sealed class ConfigStore : IConfigStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly Lock _sync = new();
    private readonly ILogger<ConfigStore> _logger;
    private readonly string _configPath;

    public ConfigStore(ILogger<ConfigStore> logger)
    {
        _logger = logger;

        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        _configPath = Path.Combine(programDataPath, "GoMicFuckYourself", "service-config.json");
    }

    public Task<ServiceConfig> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!File.Exists(_configPath))
            {
                return Task.FromResult(new ServiceConfig());
            }

            try
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<ServiceConfig>(json, SerializerOptions);
                return Task.FromResult(config ?? new ServiceConfig());
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to load service config from {ConfigPath}. Falling back to defaults.", _configPath);
                return Task.FromResult(new ServiceConfig());
            }
        }
    }

    public Task SaveAsync(ServiceConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var directory = Path.GetDirectoryName(_configPath)
                ?? throw new InvalidOperationException("Config path has no directory.");

            Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(config, SerializerOptions);
            File.WriteAllText(_configPath, json);
            return Task.CompletedTask;
        }
    }
}
