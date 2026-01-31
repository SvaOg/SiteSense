using System.Threading.Channels;
using IngestionService.Data;
using SiteSense.Shared.Models;

namespace IngestionService.Services;

internal class TelemetryProcessorService: BackgroundService
{
    private readonly ILogger<TelemetryProcessorService> _logger;
    private readonly ChannelReader<TelemetryPoint> _reader;
    private readonly TelemetryBatchWriter _batchWriter;

    public TelemetryProcessorService(
        ILogger<TelemetryProcessorService> logger,
        ChannelReader<TelemetryPoint> reader,
        TelemetryBatchWriter batchWriter)
    {
        _logger = logger;
        _reader = reader;
        _batchWriter = batchWriter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<TelemetryPoint> batch = new(500);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var timeoutsCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutsCts.CancelAfter(TimeSpan.FromSeconds(1));
            
            try
            {
                var point = await _reader.ReadAsync(timeoutsCts.Token);
                batch.Add(point);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // Timeout - not a shutdown, just no data for 1 second
            }
            
            if (batch.Count >= 500 || (batch.Count > 0 && timeoutsCts.IsCancellationRequested))
            {
                await _batchWriter.WriteBatchAsync(batch, stoppingToken);
                
                _logger.LogInformation("[Processor] Flushed batch of {BatchSize} to SQL | Queue depth: {QueueDepth}",
                    batch.Count, _reader.Count);
                
                batch.Clear();
            }
        }
    }
}
