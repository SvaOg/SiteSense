using SiteSense.Shared.Models;
using IngestionService;
using System.Threading.Channels;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<MqttSubscriberService>();

var channel = Channel.CreateBounded<TelemetryPoint>(
    new BoundedChannelOptions(10_000)
    {
        FullMode = BoundedChannelFullMode.Wait
    });

builder.Services.AddSingleton(channel.Reader);
builder.Services.AddSingleton(channel.Writer);

var host = builder.Build();
host.Run();
