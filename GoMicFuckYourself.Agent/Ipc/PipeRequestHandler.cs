using System.Text.Json;
using GoMicFuckYourself.Agent.Audio;
using GoMicFuckYourself.Agent.Enforcement;
using GoMicFuckYourself.Contracts.Configuration;

namespace GoMicFuckYourself.Agent.Ipc;

public sealed class PipeRequestHandler : IPipeRequestHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAudioController _audioController;

    private readonly IMicPolicyEngine _micPolicyEngine;

    public PipeRequestHandler(IMicPolicyEngine micPolicyEngine, IAudioController audioController)
    {
        _micPolicyEngine = micPolicyEngine;
        _audioController = audioController;
    }

    public async Task<PipeResponse> HandleAsync(PipeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Type)) return Error("Unknown", "Request type is required.");

        try
        {
            return request.Type switch
            {
                "GetStatus" => Ok(request.Type, _micPolicyEngine.GetStatus()),
                "GetConfig" => Ok(request.Type, await _micPolicyEngine.GetConfigAsync(cancellationToken)),
                "ListCaptureDevices" => Ok(request.Type, _audioController.GetCaptureDevices()),
                "SaveConfig" => await HandleSaveConfigAsync(request, cancellationToken),
                "ForceEnforce" => await HandleForceEnforceAsync(request.Type, cancellationToken),
                _ => Error(request.Type, $"Unknown request type '{request.Type}'.")
            };
        }
        catch (Exception exception)
        {
            return Error(request.Type, exception.Message);
        }
    }

    private async Task<PipeResponse> HandleSaveConfigAsync(PipeRequest request, CancellationToken cancellationToken)
    {
        var config = request.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? new ServiceConfig()
            : request.Payload.Deserialize<ServiceConfig>(SerializerOptions)
              ?? throw new InvalidOperationException("SaveConfig payload is invalid.");

        await _micPolicyEngine.SaveConfigAsync(config, cancellationToken);
        return Ok(request.Type!, await _micPolicyEngine.GetConfigAsync(cancellationToken));
    }

    private async Task<PipeResponse> HandleForceEnforceAsync(string requestType, CancellationToken cancellationToken)
    {
        await _micPolicyEngine.ForceEnforceAsync(cancellationToken);
        return Ok(requestType, _micPolicyEngine.GetStatus());
    }

    private static PipeResponse Ok(string type, object? payload)
    {
        return new PipeResponse
        {
            Success = true,
            Type = type,
            Payload = payload
        };
    }

    private static PipeResponse Error(string type, string error)
    {
        return new PipeResponse
        {
            Success = false,
            Type = type,
            Error = error
        };
    }
}