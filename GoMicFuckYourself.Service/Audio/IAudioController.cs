using GoMicFuckYourself.Contracts.Audio;

namespace GoMicFuckYourself.Service.Audio;

public interface IAudioController : IDisposable
{
    event EventHandler? CaptureDevicesChanged;
    event EventHandler<string>? DefaultCaptureDeviceChanged;
    event EventHandler<string>? DefaultCommunicationsDeviceChanged;
    event EventHandler<CaptureDeviceStateChangedEventArgs>? CaptureDeviceStateChanged;
    event EventHandler<CaptureDeviceVolumeChangedEventArgs>? CaptureDeviceVolumeChanged;

    IReadOnlyList<CaptureDeviceInfo> GetCaptureDevices();
    string? GetDefaultCaptureDeviceId(AudioPolicyRole role);
    CaptureDeviceInfo? GetCaptureDevice(string deviceId);
    void SetCaptureVolume(string deviceId, float volumePercent);
}
