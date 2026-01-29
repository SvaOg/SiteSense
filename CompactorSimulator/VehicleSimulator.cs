using MQTTnet;
using System.Text.Json;

namespace CompactorSimulator;

internal class VehicleSimulator
{
    private readonly VehicleConfig _config;
    private readonly int _siteId;
    private readonly IMqttClient _mqttClient;
    private readonly Random _random = new Random();

    // Simulation State
    private double _currentLat;
    private double _currentLon;
    private double _currentElevation;
    private bool _movingForward = true;
    private int _ticksSinceTurn = 0;

    // Constants for simulation
    // Approx 0.00001 degrees is ~1 meter. 
    // We move a tiny amount per tick to simulate ~5km/h speed
    private const double MovementStep = 0.0000015;
    private const int TicksBeforeTurn = 500; // How long a "pass" lasts before reversing

    public VehicleSimulator(VehicleConfig config, int siteId, IMqttClient mqttClient)
    {
        _config = config;
        _siteId = siteId;
        _mqttClient = mqttClient;

        // Initialize position from config
        _currentLat = config.StartLatitude;
        _currentLon = config.StartLongitude;
        _currentElevation = 100.0; // Start at 100m elevation
    }

    public async Task RunAsync(CancellationToken token)
    {
        // Calculate delay in milliseconds (e.g., 50Hz = 20ms)
        int delayMs = 1000 / _config.PublishRateHz;

        Console.WriteLine($"[Vehicle {_config.VehicleId}] Engine started. Publishing at {_config.PublishRateHz} Hz.");

        int messageCount = 0;
        while (!token.IsCancellationRequested)
        {
            // 1. Update Simulation State (Move the vehicle)
            UpdatePosition();

            // 2. Generate Telemetry Point
            var telemetry = new TelemetryPoint
            {
                VehicleId = _config.VehicleId,
                Timestamp = DateTime.UtcNow,
                SiteId = _siteId,
                Latitude = _currentLat,
                Longitude = _currentLon,
                Elevation = _currentElevation + (_random.NextDouble() * 0.1), // Slight sensor noise
                VibrationFrequency = _random.NextDouble() * 20 + 25, // 25-45 Hz
                CompactionValue = _random.NextDouble() * 70 + 30, // 30-100%
                Speed = _random.NextDouble() * 6 + 2 // 2 - 8 km/h
            };

            // 3. Serialize to JSON
            string jsonPayload = JsonSerializer.Serialize(telemetry);

            // 4. Construct MQTT Message
            // Topic: site/{siteId}/vehicle/{vehicleId}/telemetry
            string topic = $"site/{_siteId}/vehicle/{_config.VehicleId}/telemetry";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(jsonPayload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce) // QoS 0 is typical for high-freq telemetry
                .Build();

            try
            {
                // 5. Publish
                await _mqttClient.PublishAsync(message, token);

                // Optional: Console log every 100th message just to prove it's alive without spamming
                if (messageCount++ == 100)
                {
                    messageCount = 0;
                    Console.WriteLine($"[Vehicle {_config.VehicleId}] -> Sent: {jsonPayload}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Vehicle {_config.VehicleId}] Failed to publish: {ex.Message}");
            }

            // 6. Wait for next tick
            await Task.Delay(delayMs, token);
        }
    }

    private void UpdatePosition()
    {
        // Simulate driving back and forth in a straight line (common for compactors)
        if (_movingForward)
        {
            _currentLat += MovementStep;
        }
        else
        {
            _currentLat -= MovementStep;
        }

        _ticksSinceTurn++;

        // Time to reverse direction?
        if (_ticksSinceTurn >= TicksBeforeTurn)
        {
            _movingForward = !_movingForward;
            _ticksSinceTurn = 0;

            // Shift over slightly (like mowing the lawn) so we don't compact the exact same line
            _currentLon += 0.00001;
        }
    }
}