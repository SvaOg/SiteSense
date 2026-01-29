using MQTTnet;
using MQTTnet.Client;
using SiteSense.Shared.Models;
using System.Text.Json;

namespace IngestionService;

internal class MqttClientOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string[] Topics { get; set; } = [];
}

public class MqttSubscriberService : BackgroundService
{
    private readonly ILogger<MqttSubscriberService> _logger;
    private readonly IConfiguration _config;
    private int _count = 0;

    public MqttSubscriberService(ILogger<MqttSubscriberService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttOptions = _config.GetSection("Mqtt").Get<MqttClientOptions>() ?? new MqttClientOptions();
        
        var mqttFactory = new MqttFactory();
        var mqttClient = mqttFactory.CreateMqttClient();

        // In your Consumer/Worker Service
        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            string topic = e.ApplicationMessage.Topic;
            string payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            
            var telemetry = JsonSerializer.Deserialize<TelemetryPoint>(payload);

            if (_count++ > 100)
            { 
                _logger.LogInformation($"Received on {topic}: {payload}");
                _count = 0;
            }

            await Task.CompletedTask;
        };

        var clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttOptions.Host, mqttOptions.Port)
            .Build();

        await mqttClient.ConnectAsync(clientOptions, stoppingToken);

        // 1. Start the builder
        var subscribeOptionsBuilder = mqttFactory.CreateSubscribeOptionsBuilder();

        // 2. Loop through your config and add each topic
        foreach (var topicTemplate in mqttOptions.Topics)
        {
            subscribeOptionsBuilder.WithTopicFilter(f =>
            {
                f.WithTopic(topicTemplate);

                // QoS 0 (AtMostOnce): Fast, fire-and-forget. Good for high-freq telemetry.
                // QoS 1 (AtLeastOnce): Guarantees delivery, but slower (requires ACK).
                f.WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce);
            });
        }

        // 3. Build the final options object
        var subscribeOptions = subscribeOptionsBuilder.Build();

        // 4. Send the specific subscribe packet to the broker
        await mqttClient.SubscribeAsync(subscribeOptions, CancellationToken.None);

        Console.WriteLine("Subscribed to all topics successfully.");
        
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Graceful shutdown logic happens here (optional)
        }

    }
}
