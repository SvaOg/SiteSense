## Phase 3: Complete

All 5 steps done:

  1. Created bounded Channel<TelemetryPoint> (capacity 10,000) registered in DI ✓
  2. Modified MqttSubscriberService to write to channel via TryWrite (non-blocking) ✓
  3. Created TelemetryProcessorService reading from channel via await foreach ✓
  4. Added queue depth observability (Reader.Count in metrics) ✓
  5. Stress tested — confirmed backpressure works (drops messages when channel full, producer never blocks) ✓

### Key observations
- Queue depth stays near ~10 under normal load (processor faster than producer)
- With simulated slow consumer (Task.Delay), queue depth climbs to capacity
- At capacity, TryWrite returns false and messages are dropped — producer never stalls
- Dropped message counter tracks data loss for observability
