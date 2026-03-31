namespace GoMicFuckYourself.Tray.Models;

public sealed class PipeResponse<T>
{
    public bool Success { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? Error { get; init; }
    public T? Payload { get; init; }
}