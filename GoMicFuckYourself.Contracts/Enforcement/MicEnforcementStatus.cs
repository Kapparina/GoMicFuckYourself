namespace GoMicFuckYourself.Contracts.Enforcement;

public sealed record MicEnforcementStatus(
    bool IsConfigured,
    bool EnforcementEnabled,
    string? SelectedCaptureDeviceId,
    float? TargetVolumePercent,
    DateTimeOffset? LastEnforcementUtc,
    string? LastError);