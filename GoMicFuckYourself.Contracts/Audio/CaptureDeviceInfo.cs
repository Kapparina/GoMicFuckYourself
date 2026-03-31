namespace GoMicFuckYourself.Contracts.Audio;

public sealed record CaptureDeviceInfo(
    string Id,
    string FriendlyName,
    DeviceAvailability State,
    bool IsDefault,
    bool IsDefaultCommunications,
    float VolumePercent,
    bool IsMuted);