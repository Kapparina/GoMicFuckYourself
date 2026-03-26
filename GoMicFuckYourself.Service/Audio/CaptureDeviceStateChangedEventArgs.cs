using GoMicFuckYourself.Contracts.Audio;

namespace GoMicFuckYourself.Service.Audio;

public sealed class CaptureDeviceStateChangedEventArgs : EventArgs
{
    public CaptureDeviceStateChangedEventArgs(string deviceId, DeviceAvailability state)
    {
        DeviceId = deviceId;
        State = state;
    }

    public string DeviceId { get; }

    public DeviceAvailability State { get; }
}
