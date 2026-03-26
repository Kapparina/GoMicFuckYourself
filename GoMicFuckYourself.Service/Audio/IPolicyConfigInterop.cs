namespace GoMicFuckYourself.Service.Audio;

public interface IPolicyConfigInterop
{
    void SetDefaultEndpoint(string deviceId, AudioPolicyRole role);
}
