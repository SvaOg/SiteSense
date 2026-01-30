using System.Threading.Channels;
using SiteSense.Shared.Models;

namespace IngestionService.Services;

internal class TelemetryProcessorService: BackgroundService
{
    private readonly ILogger<TelemetryProcessorService> _logger;
    private readonly ChannelReader<TelemetryPoint> _reader;

    private long _messageCount = 0;

    public TelemetryProcessorService(ILogger<TelemetryProcessorService> logger, ChannelReader<TelemetryPoint> reader)
    {
        _logger = logger;
        _reader = reader;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var telemetryPoint in _reader.ReadAllAsync(stoppingToken))
        {
            _messageCount++;
            if (_messageCount % 200 == 0)
            {
                _logger.LogInformation("Processing Telemetry Point: {TelemetryPoint}, Queue Depth: {QueueDepth}", telemetryPoint, _reader.Count);
            }
        }
    }
}
