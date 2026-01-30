using MQTTnet;
using MQTTnet.Client;
using SiteSense.Shared.Models;
using System.Text.Json;
using System.Threading.Channels;

namespace IngestionService.Services;

internal class MqttBrokerOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string[] Topics { get; set; } = [];
}

public class MqttSubscriberService : BackgroundService
{
    private readonly ILogger<MqttSubscriberService> _logger;
    private readonly IConfiguration _config;
    private readonly ChannelWriter<TelemetryPoint> _writer;
    
    private long _messageCount = 0;
    private long _totalMessages = 0;
    private long _totalErrors = 0;
    private long _droppedMessages = 0;
    private DateTime _lastTimestamp = DateTime.UtcNow;

    public MqttSubscriberService(ILogger<MqttSubscriberService> logger, IConfiguration config, ChannelWriter<TelemetryPoint> writer)
    {
        _logger = logger;
        _config = config;
        _writer = writer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttOptions = _config.GetSection("Mqtt").Get<MqttBrokerOptions>() ?? new MqttBrokerOptions();

        var mqttFactory = new MqttFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        // In your Consumer/Worker Service
        mqttClient.ApplicationMessageReceivedAsync += HandleIncomingMessageAsync;

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

        _logger.LogInformation("Subscribed to all topics successfully.");

        _lastTimestamp = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(millisecondsDelay: 1000, cancellationToken: stoppingToken);

            var currentTimestamp = DateTime.UtcNow;
            var duration = currentTimestamp - _lastTimestamp;
            _lastTimestamp = currentTimestamp;

            long currentCount = Interlocked.Exchange(ref _messageCount, 0);
            _totalMessages += currentCount;
            long messageRate = (long)(currentCount / duration.TotalSeconds);

            _logger.LogInformation("[Ingestion] {messageRate} msg/sec | Total: {TotalMessages} | Errors: {TotalErrors} | Dropped: {DroppedMessages}",
                messageRate, _totalMessages, _totalErrors, _droppedMessages);
        }
    }

    private Task HandleIncomingMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        string topic = e.ApplicationMessage.Topic;
        string payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        try
        {
            // Note: Make sure TelemetryPoint is accessible here
            var telemetry = JsonSerializer.Deserialize<TelemetryPoint>(payload);
            
            if (!_writer.TryWrite(telemetry!))
            {
                Interlocked.Increment(ref _droppedMessages);
            }

            // Thread-safe increment
            Interlocked.Increment(ref _messageCount);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _totalErrors);
            _logger.LogError(ex, "Error processing message. Topic: {Topic}", topic);
        }

        return Task.CompletedTask;
    }
}

