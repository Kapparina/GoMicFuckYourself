using WixSharp;

namespace GoMicFuckYourself.Installer;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var payload = PayloadLayout.Resolve(args);
            var project = InstallerProjectFactory.Create(payload);

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
}
