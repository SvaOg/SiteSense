using IngestionService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<MqttSubscriberService>();

var host = builder.Build();
host.Run();
