Write-Host "Stopping running instances of the application..."
$processNames = @("GoMicFuckYourself.Agent", "GoMicFuckYourself.Tray")

foreach ($processName in $processNames)
{
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($processes)
    {
        foreach ($process in $processes)
        {
            try
            {
                Write-Host "Stopping process: $($process.ProcessName) (ID: $($process.Id))"
                Stop-Process -Id $process.Id -Force
            }
            catch
            {
                Write-Host "Error stopping process: $_"
            }
        }
    }
    else
    {
        Write-Host "No running instances of $processName found."
    }
}
Write-Host "All running instances have been stopped."