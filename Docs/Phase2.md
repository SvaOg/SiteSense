# Phase 2: The "Firehose" Receiver

**Goal:** Build a .NET Worker Service that subscribes to MQTT telemetry and proves it can deserialize 150 messages/second (50 Hz + 100 Hz from two compactors) without crashing or falling behind.

---

## Step 1: Create the Worker Service Project

### What You'll Learn
- The .NET Worker Service template — a long-running background process (no web server)
- How it differs from a Console App (built-in DI, logging, hosted services)

### Tasks
1. Add a new project to the solution:
   - Template: **Worker Service**
   - Name: `IngestionService`
   - Framework: .NET 9.0

2. Install NuGet packages:
   - `MQTTnet` — same library the simulator uses
   - `Microsoft.Extensions.Configuration.Binder` — if not already included

3. Add an `appsettings.json` with the MQTT broker settings:
   ```json
   {
     "Mqtt": {
       "Host": "localhost",
       "Port": 1883,
       "Topics": [
         "site/+/vehicle/+/telemetry"
       ]
     }
   }
   ```
   Note: The topic uses `+` wildcards so one subscriber picks up all sites and vehicles.

---

## Step 2: Create the MQTT Subscriber Service

### What You'll Learn
- `IHostedService` / `BackgroundService` — the .NET pattern for long-running tasks
- MQTT subscription and message handling
- JSON deserialization from a byte payload

### Tasks
1. **Create `MqttSubscriberService.cs`** that extends `BackgroundService`:
   - Override `ExecuteAsync(CancellationToken stoppingToken)`
   - Create and connect an `IMqttClient` (same pattern as the simulator)
   - Subscribe to the configured topics
   - Handle incoming messages via the `ApplicationMessageReceivedAsync` event

2. **Message handler should:**
   - Deserialize the JSON payload into a `TelemetryPoint`
   - Log a counter (e.g., total messages received, messages per second)
   - NOT persist to a database yet — just prove we can keep up

3. **Register the service** in `Program.cs`:
   ```csharp
   builder.Services.AddHostedService<MqttSubscriberService>();
   ```

---

## Step 3: Share the TelemetryPoint Model

### What You'll Learn
- How to share code between projects in the same solution
- When to create a shared library vs. duplicate code

### Tasks
1. **Create a Class Library project:**
   - Name: `SiteSense.Shared`
   - Move `TelemetryPoint.cs` here (make it `public`)

2. **Add project references:**
   - `CompactorSimulator` → references `SiteSense.Shared`
   - `IngestionService` → references `SiteSense.Shared`

3. Update the `CompactorSimulator` to use the shared model (remove its local copy)

---

## Step 4: Add Metrics — Prove You Can Keep Up

### What You'll Learn
- Simple performance measurement in .NET
- Why measuring throughput matters before adding more complexity

### Tasks
1. **Track messages per second** in the subscriber:
   - Keep a counter that increments on each message received
   - Every second (use a `Timer` or `Stopwatch`), log the count and reset
   - Example output: `[Ingestion] Received 150 msg/sec (total: 4500)`

2. **Track deserialization failures:**
   - If JSON is malformed, log the error but don't crash
   - Track the failure count separately

---

## Step 5: Run End-to-End

### Tasks
1. Start the Mosquitto broker (should already be running)
2. Start the `IngestionService` (F5 or from terminal)
3. Start the `CompactorSimulator` (second instance of VS, or from terminal)
4. Watch the ingestion service logs — you should see ~150 msg/sec

### Experiments
- What happens if you add a third vehicle at 100 Hz in the simulator config? Can the service keep up at 250 msg/sec?
- What happens if you stop the simulator? Does the service stay healthy?
- What happens if you stop and restart the broker? Does the service recover?

---

## Success Criteria

- [ ] IngestionService runs as a Worker Service
- [ ] Subscribes to wildcard MQTT topic and receives from all vehicles
- [ ] Successfully deserializes every message into TelemetryPoint
- [ ] Logs throughput (msg/sec) to console
- [ ] Handles malformed messages without crashing
- [ ] TelemetryPoint model is shared between projects via class library
- [ ] Can sustain 150+ msg/sec without memory growth

---

## Key Concepts to Understand

1. **BackgroundService vs IHostedService:**
   - `IHostedService` has `StartAsync` and `StopAsync` — you manage everything
   - `BackgroundService` gives you `ExecuteAsync` — simpler for "run a loop forever" scenarios
   - Use `BackgroundService` for this project

2. **Why a Worker Service (not a Console App)?**
   - Built-in dependency injection
   - Built-in configuration (appsettings.json, env vars)
   - Built-in logging
   - Graceful shutdown support
   - Can later be deployed as a Windows Service or Linux daemon

3. **MQTT Subscriber Pattern:**
   - Subscribe is declarative (say what topics you want)
   - Messages arrive via an event/callback — you don't poll
   - This is fundamentally different from HTTP request/response

---

## Next Phase Preview

In Phase 3, you'll add a `Channel<TelemetryPoint>` between the MQTT subscriber and a processing pipeline. This decouples "receiving fast" from "processing at your own pace" — the key to handling backpressure.
