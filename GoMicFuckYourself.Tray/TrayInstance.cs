namespace GoMicFuckYourself.Tray;

internal static class TrayInstance
{
    private const string MutexName = @"Global\GoMicFuckYourself.Tray";

    public static bool TryAcquire(out IDisposable? handle)
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (createdNew)
        {
            handle = mutex;
            return true;
        }

        mutex.Dispose();
        handle = null;
        return false;
    }
}
