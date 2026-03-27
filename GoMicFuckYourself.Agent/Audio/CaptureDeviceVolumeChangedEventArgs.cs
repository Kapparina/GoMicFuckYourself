namespace GoMicFuckYourself.Agent.Audio;

public sealed class CaptureDeviceVolumeChangedEventArgs : EventArgs
{
    public CaptureDeviceVolumeChangedEventArgs(string deviceId, float volumePercent, bool isMuted)
    {
        DeviceId = deviceId;
        VolumePercent = volumePercent;
        IsMuted = isMuted;
    }

    public string DeviceId { get; }

    public float VolumePercent { get; }

    public bool IsMuted { get; }
}
