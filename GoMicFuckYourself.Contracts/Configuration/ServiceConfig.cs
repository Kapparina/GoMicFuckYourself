namespace GoMicFuckYourself.Contracts.Configuration;

public sealed class ServiceConfig
{
    public string? SelectedCaptureDeviceId { get; init; }

    public string? SelectedCaptureDeviceName { get; init; }

    public float? TargetVolumePercent { get; init; } = 100f;

    public bool EnforcementEnabled { get; init; } = true;
}
