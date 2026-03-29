using System.Text.Json;
using GoMicFuckYourself.Contracts.Configuration;

namespace GoMicFuckYourself.Tray;

internal static class SetupStateDetector
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool IsSetupPending()
    {
        try
        {
            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var configPath = Path.Combine(programDataPath, "GoMicFuckYourself", "agent-config.json");
            if (!File.Exists(configPath))
            {
                return true;
            }

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<ServiceConfig>(json, JsonOptions);
            return string.IsNullOrWhiteSpace(config?.SelectedCaptureDeviceId);
        }
        catch
        {
            return true;
        }
    }
}
