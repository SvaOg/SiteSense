using SiteSense.Shared.Models;
using System.Threading.Channels;
using IngestionService.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<MqttSubscriberService>();
builder.Services.AddHostedService<TelemetryProcessorService>();

var channel = Channel.CreateBounded<TelemetryPoint>(
    new BoundedChannelOptions(10_000)
    {
        FullMode = BoundedChannelFullMode.Wait
    });

builder.Services.AddSingleton(channel.Reader);
builder.Services.AddSingleton(channel.Writer);

var host = builder.Build();
host.Run();
