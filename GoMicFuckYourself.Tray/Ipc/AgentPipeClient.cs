using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using GoMicFuckYourself.Contracts.Audio;
using GoMicFuckYourself.Contracts.Configuration;
using GoMicFuckYourself.Contracts.Enforcement;
using GoMicFuckYourself.Contracts.Ipc;
using GoMicFuckYourself.Tray.Models;

namespace GoMicFuckYourself.Tray;

public interface IAgentPipeClient : IDisposable
{
    Task<PipeResponse<MicEnforcementStatus>> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<PipeResponse<ServiceConfig>> GetConfigAsync(CancellationToken cancellationToken = default);
    Task<PipeResponse<List<CaptureDeviceInfo>>> ListCaptureDevicesAsync(CancellationToken cancellationToken = default);

    Task<PipeResponse<ServiceConfig>> SaveConfigAsync(ServiceConfig config,
        CancellationToken cancellationToken = default);

    Task<PipeResponse<MicEnforcementStatus>> ForceEnforceAsync(CancellationToken cancellationToken = default);
}

public sealed class AgentPipeClient : IAgentPipeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<PipeResponse<MicEnforcementStatus>> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync<MicEnforcementStatus>("GetStatus", null, cancellationToken);
    }

    public async Task<PipeResponse<ServiceConfig>> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync<ServiceConfig>("GetConfig", null, cancellationToken);
    }

    public async Task<PipeResponse<List<CaptureDeviceInfo>>> ListCaptureDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        return await SendAsync<List<CaptureDeviceInfo>>("ListCaptureDevices", null, cancellationToken);
    }

    public async Task<PipeResponse<ServiceConfig>> SaveConfigAsync(ServiceConfig config,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync<ServiceConfig>("SaveConfig", config, cancellationToken);
    }

    public async Task<PipeResponse<MicEnforcementStatus>> ForceEnforceAsync(
        CancellationToken cancellationToken = default)
    {
        return await SendAsync<MicEnforcementStatus>("ForceEnforce", null, cancellationToken);
    }

    public void Dispose()
    {
    }

    private static async Task<PipeResponse<T>> SendAsync<T>(string type, object? payload,
        CancellationToken cancellationToken)
    {
        using var pipe =
            new NamedPipeClientStream(".", PipeConstants.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await ConnectAsync(pipe, cancellationToken);

        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);

        var requestJson = JsonSerializer.Serialize(new PipeRequest { Type = type, Payload = payload }, JsonOptions);
        await writer.WriteLineAsync(requestJson);

        var responseJson = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(responseJson))
            throw new InvalidOperationException("The agent returned an empty response.");

        var envelope = JsonSerializer.Deserialize<PipeResponseEnvelope>(responseJson, JsonOptions)
                       ?? throw new InvalidOperationException("The agent response was invalid.");

        T? data = default;
        if (envelope.Payload.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            data = envelope.Payload.Deserialize<T>(JsonOptions);

        return new PipeResponse<T>
        {
            Success = envelope.Success,
            Type = envelope.Type ?? type,
            Error = envelope.Error,
            Payload = data
        };
    }

    private static async Task ConnectAsync(NamedPipeClientStream pipe, CancellationToken cancellationToken)
    {
        TimeoutException? lastTimeout = null;

        for (var attempt = 0; attempt < 3; attempt++)
            try
            {
                await pipe.ConnectAsync(1500, cancellationToken);
                return;
            }
            catch (TimeoutException exception)
            {
                lastTimeout = exception;
                await Task.Delay(300, cancellationToken);
            }

        throw lastTimeout ?? new TimeoutException("The agent did not accept the pipe connection in time.");
    }

    private sealed class PipeRequest
    {
        public string? Type { get; init; }
        public object? Payload { get; init; }
    }

    private sealed class PipeResponseEnvelope
    {
        public bool Success { get; init; }
        public string? Type { get; init; }
        public string? Error { get; init; }
        public JsonElement Payload { get; init; }
    }
}