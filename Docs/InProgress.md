## Phase 2: Complete

All 5 steps done:

  1. Created the Worker Service project (IngestionService) ✓
  2. Built the MQTT subscriber (MqttSubscriberService as BackgroundService) ✓
  3. Shared the TelemetryPoint model via SiteSense.Shared class library ✓
  4. Added throughput metrics (msg/sec, total count, error tracking with Interlocked) ✓
  5. Ran end-to-end — ingestion service keeps up with multiple vehicles ✓

### Code review fixes applied
- Renamed config class from MqttClientOptions to avoid collision with MQTTnet type
- Added `using var` on IMqttClient for proper disposal
- Fixed structured logging in error handler
- Made message handler non-async (returns Task.CompletedTask)

### Known limitation
- VehicleSimulator: Windows timer resolution caps single-vehicle rate at ~64 msg/sec
- Workaround: use multiple vehicles to reach target aggregate throughput
- Simulator uses 100ms sleep with catch-up burst pattern (accurate aggregate rate, bursty delivery)
