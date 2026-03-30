using GoMicFuckYourself.Agent.Audio;
using GoMicFuckYourself.Agent.Configuration;
using GoMicFuckYourself.Agent.Enforcement;
using GoMicFuckYourself.Agent.Ipc;
using GoMicFuckYourself.Agent.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

const string mutexName = @"Local\GoMicFuckYourself.Agent";

using var mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
if (!createdNew)
{
    return;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddEventLog(settings =>
{
    settings.LogName = EventLogConstants.LogName;
    settings.SourceName = EventLogConstants.SourceName;
});
builder.Logging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Information);

builder.Services.AddSingleton<IAudioController, WindowsAudioController>();
builder.Services.AddSingleton<IPolicyConfigInterop, PolicyConfigInterop>();
builder.Services.AddSingleton<IConfigStore, ConfigStore>();
builder.Services.AddSingleton<IMicPolicyEngine, MicPolicyEngine>();
builder.Services.AddHostedService<AgentLifecycleHostedService>();
builder.Services.AddHostedService<MicPolicyBackgroundService>();
builder.Services.AddSingleton<IPipeRequestHandler, PipeRequestHandler>();
builder.Services.AddHostedService<PipeServer>();

var app = builder.Build();
await app.RunAsync();
