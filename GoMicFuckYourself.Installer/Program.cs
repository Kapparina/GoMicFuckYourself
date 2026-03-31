using WixSharp;

namespace GoMicFuckYourself.Installer;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var payload = PayloadLayout.Resolve(args);
            var version = ResolveVersion(args);
            var project = InstallerProjectFactory.Create(payload, version);

            Compiler.BuildMsi(project);

            Console.WriteLine($"MSI generated in '{project.OutDir}'.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static string ResolveVersion(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count - 1; index++)
            if (string.Equals(args[index], "--version", StringComparison.OrdinalIgnoreCase))
            {
                var version = args[index + 1];
                if (!string.IsNullOrWhiteSpace(version)) return version.Trim();
            }

        return InstallerConstants.DefaultVersion;
    }
}