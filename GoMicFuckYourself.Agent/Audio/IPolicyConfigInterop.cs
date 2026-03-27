namespace GoMicFuckYourself.Agent.Audio;

public interface IPolicyConfigInterop
{
    void SetDefaultEndpoint(string deviceId, AudioPolicyRole role);
}
