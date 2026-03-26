namespace GoMicFuckYourself.Installer;

internal sealed class PayloadLayout
{
    public string OutputRoot { get; set; } = string.Empty;
    public string PayloadRoot { get; set; } = string.Empty;
    public string ServicePayloadRoot { get; set; } = string.Empty;
    public string TrayPayloadRoot { get; set; } = string.Empty;
    public string ServiceExecutablePath { get; set; } = string.Empty;
    public string TrayExecutablePath { get; set; } = string.Empty;
    public string DefaultConfigPath { get; set; } = string.Empty;

    public static PayloadLayout Resolve(string[] args)
    {
        var outputRoot = AppContext.BaseDirectory;
        var payloadRoot = GetOption(args, "--payload-root")
                          ?? Path.GetFullPath(Path.Combine(outputRoot, "payload"));

        var servicePayloadRoot = Path.Combine(payloadRoot, "Service");
        var trayPayloadRoot = Path.Combine(payloadRoot, "Tray");
        var defaultConfigPath = Path.Combine(outputRoot, "Assets", "service-config.json");

        var serviceExecutablePath = Path.Combine(servicePayloadRoot, "GoMicFuckYourself.Service.exe");
        var trayExecutablePath = Path.Combine(trayPayloadRoot, "GoMicFuckYourself.Tray.exe");

        EnsureDirectoryExists(servicePayloadRoot, "service payload");
        EnsureDirectoryExists(trayPayloadRoot, "tray payload");
        EnsureFileExists(serviceExecutablePath, "service executable");
        EnsureFileExists(trayExecutablePath, "tray executable");
        EnsureFileExists(defaultConfigPath, "default config asset");

        return new PayloadLayout
        {
            OutputRoot = outputRoot,
            PayloadRoot = payloadRoot,
            ServicePayloadRoot = servicePayloadRoot,
            TrayPayloadRoot = trayPayloadRoot,
            ServiceExecutablePath = serviceExecutablePath,
            TrayExecutablePath = trayExecutablePath,
            DefaultConfigPath = defaultConfigPath
        };
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static void EnsureDirectoryExists(string path, string description)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"The {description} directory was not found: {path}");
        }
    }

    private static void EnsureFileExists(string path, string description)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The {description} file was not found.", path);
        }
    }
}
