5 steps:

  1. Create the Worker Service project — IngestionService using the Worker Service template
  2. Build the MQTT subscriber — a BackgroundService that connects, subscribes with wildcard topics, and deserializes incoming
    messages
  3. Share the TelemetryPoint model — extract it into a SiteSense.Shared class library referenced by both projects
  4. Add throughput metrics — track and log messages per second to prove you can keep up with 150 msg/sec
  5. Run end-to-end — both projects running together, with experiments to stress-test

  The key new concept here is BackgroundService — the .NET pattern for long-running processes. It gives you DI, configuration,
  logging, and graceful shutdown out of the box, which is why we use it instead of another Console App.