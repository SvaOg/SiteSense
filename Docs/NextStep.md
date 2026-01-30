## Phase 3: Channel-Based Decoupling

Add a `Channel<TelemetryPoint>` between the MQTT subscriber and a processing pipeline. This decouples "receiving fast" from "processing at your own pace" — the key to handling backpressure.

### Key concept
Right now the MQTT handler deserializes and discards. In Phase 3, it will write to a Channel, and a separate BackgroundService will read from that Channel and process messages. This is the Producer-Consumer pattern using System.Threading.Channels.

### Why this matters
- The MQTT callback must return quickly — if it blocks, the broker's message queue backs up
- Database writes (Phase 4) will be slower than message arrival
- The Channel acts as an in-memory buffer, letting the receiver stay fast while the processor works at its own pace
- Bounded channels add backpressure — if the buffer fills, the producer slows down rather than consuming unlimited memory

### See Phase 3 doc (Docs/Phase2.md or upcoming Phase3.md) for detailed steps.
