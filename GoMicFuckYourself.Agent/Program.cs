using GoMicFuckYourself.Agent.Audio;
using GoMicFuckYourself.Agent.Configuration;
using GoMicFuckYourself.Agent.Enforcement;
using GoMicFuckYourself.Agent.Ipc;

const string mutexName = @"Local\GoMicFuckYourself.Agent";

using var mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
if (!createdNew)
{
    return;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IAudioController, WindowsAudioController>();
builder.Services.AddSingleton<IPolicyConfigInterop, PolicyConfigInterop>();
builder.Services.AddSingleton<IConfigStore, ConfigStore>();
builder.Services.AddSingleton<IMicPolicyEngine, MicPolicyEngine>();
builder.Services.AddHostedService<MicPolicyBackgroundService>();
builder.Services.AddSingleton<IPipeRequestHandler, PipeRequestHandler>();
builder.Services.AddHostedService<PipeServer>();

var app = builder.Build();
await app.RunAsync();
