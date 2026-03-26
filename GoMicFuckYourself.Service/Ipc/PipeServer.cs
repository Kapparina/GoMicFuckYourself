using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using GoMicFuckYourself.Contracts.Ipc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GoMicFuckYourself.Service.Ipc;

public sealed class PipeServer : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IPipeRequestHandler _requestHandler;
    private readonly ILogger<PipeServer> _logger;

    public PipeServer(IPipeRequestHandler requestHandler, ILogger<PipeServer> logger)
    {
        _requestHandler = requestHandler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var server = CreateServer();

            try
            {
                await server.WaitForConnectionAsync(stoppingToken);
                _ = Task.Run(() => HandleConnectionAsync(server, stoppingToken), CancellationToken.None);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                server.Dispose();
                break;
            }
            catch (Exception exception)
            {
                server.Dispose();
                _logger.LogError(exception, "Named pipe listener failed.");
            }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        await using var _ = server.ConfigureAwait(false);

        try
        {
            using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            using var writer = new StreamWriter(server, new UTF8Encoding(false), leaveOpen: true)
            {
                AutoFlush = true
            };

            while (!cancellationToken.IsCancellationRequested && server.IsConnected)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    break;
                }

                PipeResponse response;
                try
                {
                    var request = JsonSerializer.Deserialize<PipeRequest>(line, SerializerOptions)
                        ?? throw new InvalidOperationException("Request body is empty.");
                    response = await _requestHandler.HandleAsync(request, cancellationToken);
                }
                catch (Exception exception)
                {
                    response = new PipeResponse
                    {
                        Success = false,
                        Type = "InvalidRequest",
                        Error = exception.Message
                    };
                }

                var json = JsonSerializer.Serialize(response, SerializerOptions);
                await writer.WriteLineAsync(json);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Named pipe client handling failed.");
        }
    }

    private static NamedPipeServerStream CreateServer()
    {
        return new NamedPipeServerStream(
            PipeConstants.PipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);
    }
}
