namespace GoMicFuckYourself.Agent.Ipc;

public interface IPipeRequestHandler
{
    Task<PipeResponse> HandleAsync(PipeRequest request, CancellationToken cancellationToken);
}
