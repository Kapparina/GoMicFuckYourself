using Microsoft.Win32;
using WixToolset.Dtf.WindowsInstaller;

namespace GoMicFuckYourself.Installer;

public static class UninstallCleanupActions
{
    [CustomAction]
    public static ActionResult RemoveCurrentUserAutorun(Session session)
    {
        const string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string startupApprovedRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        const string trayAutorunName = "GoMicFuckYourself.Tray";

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(runKeyPath, writable: true);
            using var startupApprovedKey = Registry.CurrentUser.CreateSubKey(startupApprovedRunKeyPath, writable: true);
            key?.DeleteValue(trayAutorunName, throwOnMissingValue: false);
            startupApprovedKey?.DeleteValue(trayAutorunName, throwOnMissingValue: false);
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            session.Log($"Failed to remove current-user autorun entry: {exception}");
            return ActionResult.Success;
        }
    }
}
