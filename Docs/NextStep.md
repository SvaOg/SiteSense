## Step 4: Add Throughput Metrics.

We need to prove the ingestion service can keep up with 150 msg/sec. Right now you're logging messages, but you need structured throughput tracking.

### What to do

In your message handler, add:

  1. A counter (int or long) that increments on every message received
  2. A Stopwatch (or track DateTime.UtcNow) to measure elapsed time
  3. Every second, log the rate and reset the counter. Something like:
    [Ingestion] 152 msg/sec | Total: 4,560
  3. You can check elapsed time inside the message handler itself — if more than 1 second has passed since last log, print and
    reset. Or use a separate PeriodicTimer in ExecuteAsync.
  4. Track deserialization failures separately. Wrap your JsonSerializer.Deserialize in a try/catch — if it fails, increment a
    failure counter and log the error, but don't crash. Log something like:
    [Ingestion] Deserialization error (total failures: 3): ...

  The key idea: measure before you optimize. Before adding Channels or database writes in later phases, you want a baseline number
  proving the receiver alone can handle the load.

  After that — Step 5: Run End-to-End

  Once metrics are in, run both projects together and verify you see ~150 msg/sec in the logs. Then try some experiments:

  - Add a third vehicle in the simulator's appsettings.json at 100Hz — can it handle 250 msg/sec?
  - Stop the simulator — does the service stay healthy?
  - Stop and restart the broker — does the service recover?