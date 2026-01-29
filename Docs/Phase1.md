# Phase 1: Infrastructure & The "Mock" Hardware

**Goal:** Get messages flowing from a simulated compactor through an MQTT broker. By the end, you'll see telemetry data "flying through the air."

---

## Step 1: Set Up the MQTT Broker (Mosquitto)

### What You'll Learn
- What an MQTT broker does (it's the "post office" that routes messages between publishers and subscribers)
- How to run infrastructure locally using Docker

### Tasks
1. **Install Docker Desktop** (if not already installed)
   - Download from https://www.docker.com/products/docker-desktop/
   - Verify installation: `docker --version`

2. **Create a `docker-compose.yml`** in the solution root with Mosquitto configuration:
   - Use the official `eclipse-mosquitto` image
   - Expose port 1883 (MQTT default port)
   - Mount a local config file for custom settings

3. **Create Mosquitto config file** (`mosquitto/config/mosquitto.conf`):
   - Allow anonymous connections (for local dev only)
   - Set up a listener on port 1883

4. **Start the broker:**
   ```
   docker-compose up -d
   ```

5. **Verify it's running:**
   ```
   docker ps
   ```

---

## Step 2: Test the Broker Manually

### What You'll Learn
- MQTT concepts: topics, publish, subscribe
- How to use command-line tools to debug MQTT

### Tasks
1. **Install an MQTT client** for testing:
   - Option A: Use `mosquitto_pub` and `mosquitto_sub` (comes with Mosquitto)
   - Option B: Install MQTT Explorer (GUI tool) - https://mqtt-explorer.com/

2. **Subscribe to a test topic** (in one terminal):
   ```
   docker exec -it <container_name> mosquitto_sub -t "test/topic"
   ```

3. **Publish a test message** (in another terminal):
   ```
   docker exec -it <container_name> mosquitto_pub -t "test/topic" -m "Hello MQTT!"
   ```

4. **Verify** you see "Hello MQTT!" appear in the subscriber terminal

---

## Step 3: Create the CompactorSimulator Project

### What You'll Learn
- Setting up a .NET Console Application
- The MQTTnet library for .NET
- Designing a telemetry data model

### Tasks
1. **Add a new Console App project** to your solution:
   - Name: `CompactorSimulator`
   - Framework: .NET 9.0

2. **Install the MQTTnet NuGet package:**
   ```
   dotnet add package MQTTnet
   ```

3. **Create the telemetry data model** (`TelemetryPoint.cs`):
   ```csharp
   public class TelemetryPoint
   {
       public DateTime Timestamp { get; set; }
       public string VehicleId { get; set; }
       public double Latitude { get; set; }
       public double Longitude { get; set; }
       public double Elevation { get; set; }
       public double VibrationFrequency { get; set; }  // Hz
       public double CompactionValue { get; set; }     // 0-100%
   }
   ```

4. **Implement the MQTT publisher** (`Program.cs`):
   - Connect to the local broker (`localhost:1883`)
   - Generate fake GPS coordinates (simulate movement along a path)
   - Generate random vibration/compaction values
   - Publish JSON to topic: `site/1/vehicle/compactor-01/telemetry`
   - Target rate: 50-100 messages per second

---

## Step 4: Run and Verify

### What You'll Learn
- End-to-end verification of the data pipeline
- Using MQTT tools to inspect live traffic

### Tasks
1. **Start a subscriber** to watch the telemetry topic:
   ```
   docker exec -it <container_name> mosquitto_sub -t "site/+/vehicle/+/telemetry"
   ```
   (The `+` is a single-level wildcard in MQTT)

2. **Run the CompactorSimulator** from Visual Studio

3. **Verify** you see JSON messages streaming in the subscriber terminal

4. **Experiment:**
   - Try different publish rates
   - Watch CPU/memory usage
   - Stop and restart the simulator - what happens to the subscriber?

---

## Success Criteria

- [ ] Mosquitto broker running in Docker
- [ ] Can manually publish/subscribe using command-line tools
- [ ] CompactorSimulator project created and publishing telemetry
- [ ] Can see JSON telemetry messages arriving at 50+ messages/second

---

## Key Concepts to Understand

1. **MQTT QoS Levels:**
   - QoS 0: Fire and forget (fastest, no guarantee)
   - QoS 1: At least once (may duplicate)
   - QoS 2: Exactly once (slowest, guaranteed)
   - *For this project, start with QoS 0 for speed*

2. **Topic Hierarchy:**
   - Topics are like paths: `site/1/vehicle/compactor-01/telemetry`
   - Wildcards: `+` (single level), `#` (multi-level)
   - Design topics to be filterable

3. **Why Decouple with a Broker?**
   - Producer doesn't need to know about consumers
   - Multiple consumers can subscribe to the same data
   - Broker handles buffering if consumer is slow (to a point)

---

## Next Phase Preview

In Phase 2, you'll create the **Ingestion Service** that subscribes to these messages and proves it can handle the firehose of data without crashing.
