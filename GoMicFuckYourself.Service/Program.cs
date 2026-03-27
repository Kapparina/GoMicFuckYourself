using GoMicFuckYourself.Service.Audio;
using GoMicFuckYourself.Service.Configuration;
using GoMicFuckYourself.Service.Enforcement;
using GoMicFuckYourself.Service.Ipc;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "GoMicFuckYourself.Service";
});

builder.Services.AddSingleton<IAudioController, WindowsAudioController>();
builder.Services.AddSingleton<IPolicyConfigInterop, PolicyConfigInterop>();
builder.Services.AddSingleton<IConfigStore, ConfigStore>();
builder.Services.AddSingleton<IMicPolicyEngine, MicPolicyEngine>();
builder.Services.AddHostedService<MicPolicyBackgroundService>();
builder.Services.AddSingleton<IPipeRequestHandler, PipeRequestHandler>();
builder.Services.AddHostedService<PipeServer>();

var app = builder.Build();
await app.RunAsync();
