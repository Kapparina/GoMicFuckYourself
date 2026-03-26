namespace GoMicFuckYourself.Service.Ipc;

public interface IPipeRequestHandler
{
    Task<PipeResponse> HandleAsync(PipeRequest request, CancellationToken cancellationToken);
}
