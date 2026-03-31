using System.Text.Json;

namespace GoMicFuckYourself.Agent.Ipc;

public sealed class PipeRequest
{
    public string? Type { get; init; }

    public JsonElement Payload { get; init; }
}