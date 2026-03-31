namespace GoMicFuckYourself.Agent.Ipc;

public sealed class PipeResponse
{
    public required bool Success { get; init; }

    public required string Type { get; init; }

    public string? Error { get; init; }

    public object? Payload { get; init; }
}