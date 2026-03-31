using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using GoMicFuckYourself.Contracts.Ipc;

namespace GoMicFuckYourself.Agent.Ipc;

public sealed class PipeServer(IPipeRequestHandler requestHandler, ILogger<PipeServer> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
                await server.DisposeAsync();
                break;
            }
            catch (IOException exception) when (stoppingToken.IsCancellationRequested ||
                                                exception.Message.Contains("pipe is being closed",
                                                    StringComparison.OrdinalIgnoreCase))
            {
                await server.DisposeAsync();
                logger.LogWarning(exception, "Named pipe listener stopped while the pipe was being closed.");
                break;
            }
            catch (Exception exception)
            {
                await server.DisposeAsync();
                logger.LogError(exception, "Named pipe listener failed.");
            }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        await using var _ = server.ConfigureAwait(false);

        try
        {
            using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
            await using var writer = new StreamWriter(server, new UTF8Encoding(false), leaveOpen: true);
            writer.AutoFlush = true;

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
                    response = await requestHandler.HandleAsync(request, cancellationToken);
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
            logger.LogError(exception, "Named pipe client handling failed.");
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