### Step-by-Step Implementation Plan

Here is how we will attack this. I recommend we go in this order:

#### Phase 1: Infrastructure & The "Mock" Hardware

- Get a local MQTT broker running.
- Write the `CompactorSimulator` (Producer) to spam us with JSON data.
- *Goal:* See messages flying through the air.

#### Phase 2: The "Firehose" Receiver

- Create the main Worker Service.
- Connect to MQTT using the **MQTTnet** library (industry standard for .NET).
- Parse the incoming JSON.
- *Goal:* Verify we can deserialize 100 messages/second without crashing.

#### Phase 3: The Decoupled Buffer (The "Senior" Part)

- Implement `Channel<TelemetryPoint>`.
- Connect the MQTT receiver (Writer) to the Channel.
- Create a simple "Console Logger" consumer to prove the decoupling works.

#### Phase 4: High-Performance Persistence

- Replace the logger with a real **SQL Batch Writer**.
- We will use `SqlBulkCopy` or Dapper to insert 1,000 rows at a time, rather than 1,000 individual `INSERT` statements.