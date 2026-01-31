# SiteSense

A high-throughput telemetry ingestion system for construction equipment. Simulates Smart Soil Compactors streaming GNSS position and vibration data at 50-100Hz, ingested through MQTT and persisted to SQL Server using a Producer-Consumer architecture.

## Architecture

```
CompactorSimulator          MQTT Broker           IngestionService              SQL Server
(Edge Devices)           (Mosquitto)           (Worker Service)              (Docker)

compactor-01 (100Hz) ─┐                    ┌─ MqttSubscriberService
compactor-02 (100Hz) ─┼──► port 1883 ──►  │  (deserialize + enqueue)
compactor-03  (50Hz) ─┘                    │         │
                                           │   Channel<TelemetryPoint>
                          topic:           │   (bounded, 10k capacity)
                          site/{siteId}/   │         │
                          vehicle/{id}/    │  TelemetryProcessorService
                          telemetry        │  (batch 500 items or 1s)
                                           │         │
                                           │  TelemetryBatchWriter
                                           └─ (SqlBulkCopy) ──────────► TelemetryPoints
```

**Key design decisions:**

- **MQTT (QoS 0)** between simulator and ingestion — fire-and-forget for maximum throughput
- **Bounded Channel (10,000 items)** decouples the fast MQTT receiver from slower database writes
- **TryWrite (non-blocking)** in the MQTT handler — drops messages when the channel is full rather than stalling the receiver
- **SqlBulkCopy** for batch persistence — one bulk operation per 500 rows instead of 500 individual INSERTs

## Projects

| Project | Type | Purpose |
|---|---|---|
| `CompactorSimulator` | Console App | Simulates multiple vehicles publishing telemetry to MQTT |
| `IngestionService` | Worker Service | Subscribes to MQTT, buffers in a channel, batch-writes to SQL |
| `SiteSense.Shared` | Class Library | Shared `TelemetryPoint` model |

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Getting Started

### 1. Start infrastructure

```bash
docker-compose up -d
```

This starts:
- **Mosquitto** MQTT broker on port 1883
- **SQL Server 2022** on port 1433

### 2. Initialize the database

Run `Scripts/InitDatabase.sql` against the SQL Server instance. Using SSMS, connect to `localhost,1433` with the SA credentials from your `.env` file, then execute the script.

### 3. Configure the connection string

The `appsettings.json` contains a placeholder password. Store the real password using .NET User Secrets:

```bash
cd IngestionService
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=SiteSense;User Id=sa;Password=<your-password>;TrustServerCertificate=True;"
```

### 4. Run the services

Start the ingestion service and simulator in separate terminals:

```bash
dotnet run --project IngestionService/IngestionService.csproj
```

```bash
dotnet run --project CompactorSimulator/CompactorSimulator.csproj
```

### 5. Verify

You should see output like:

```
[Ingestion] 250 msg/sec | Total: 1500 | Errors: 0 | Dropped: 0
[Processor] Batch of 500 written in 38 ms | Queue depth: 12
```

Query the database to confirm data landed:

```sql
SELECT COUNT(*) FROM TelemetryPoints;
SELECT TOP 10 * FROM TelemetryPoints ORDER BY Timestamp DESC;
```

## Configuration

### Simulator (`CompactorSimulator/appsettings.json`)

```json
{
  "Simulation": {
    "SiteId": 1,
    "Vehicles": [
      {
        "VehicleId": "compactor-01",
        "StartLatitude": 47.6062,
        "StartLongitude": -122.3321,
        "PublishRateHz": 100
      }
    ]
  }
}
```

Add or remove vehicles to change the aggregate throughput. Each vehicle publishes independently at its configured rate.

### Ingestion Service (`IngestionService/appsettings.json`)

```json
{
  "Mqtt": {
    "Host": "localhost",
    "Port": 1883,
    "Topics": [ "site/+/vehicle/+/telemetry" ]
  }
}
```

The `+` wildcard subscribes to all sites and vehicles with a single subscription.

## Database Schema

```sql
CREATE TABLE TelemetryPoints (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    Timestamp           DATETIME2(3) NOT NULL,
    VehicleId           NVARCHAR(50) NOT NULL,
    Latitude            FLOAT NOT NULL,
    Longitude           FLOAT NOT NULL,
    Elevation           FLOAT NOT NULL,
    VibrationFrequency  FLOAT NOT NULL,
    CompactionValue     FLOAT NOT NULL,
    IngestedAt          DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()
);
```

`IngestedAt` is auto-set by SQL Server on insert — compare with `Timestamp` to measure end-to-end latency.

## How It Works

### Data Flow

1. **CompactorSimulator** generates telemetry (GPS coordinates, vibration, compaction) and publishes JSON to MQTT at 50-100Hz per vehicle
2. **MqttSubscriberService** receives messages, deserializes JSON into `TelemetryPoint`, and writes to a bounded channel using `TryWrite`
3. **TelemetryProcessorService** reads from the channel, accumulates messages into batches of 500 (or flushes after 1 second of inactivity)
4. **TelemetryBatchWriter** maps the batch into a `DataTable` and bulk-inserts via `SqlBulkCopy`

### Backpressure

The bounded channel (capacity 10,000) is the backpressure mechanism:

- **Queue depth near 0** — processor is keeping up, no buffering needed
- **Queue depth growing** — processor is slower than producer, channel is absorbing the difference
- **Queue depth at 10,000** — channel is full, `TryWrite` returns `false`, messages are dropped and counted

At 250 msg/sec, the 10,000-item buffer provides ~40 seconds of headroom before any data loss.

### Batching

The processor uses two flush triggers:

- **Size-based (500 items)** — handles high-throughput periods efficiently
- **Time-based (1 second)** — ensures data doesn't sit in memory during quiet periods

This is implemented using `CancellationTokenSource.CreateLinkedTokenSource` with a timeout on `ChannelReader.ReadAsync`.

## Tech Stack

- **.NET 9.0** — target framework for all projects
- **MQTTnet 4.3.7** — MQTT client library
- **System.Threading.Channels** — in-memory producer-consumer queue
- **SqlBulkCopy** (via Microsoft.Data.SqlClient) — high-performance bulk inserts
- **Eclipse Mosquitto 2** — MQTT broker (Docker)
- **SQL Server 2022** — database (Docker)

## Known Limitations

- **Windows timer resolution** caps a single vehicle's publish rate at ~64 msg/sec. Use multiple vehicles to reach higher aggregate throughput.
- **No retry on failed batches** — failed SQL writes are logged and dropped. A production system would add retry logic or a dead-letter queue.
- **No MQTT reconnection logic** — if the broker goes down, the service must be restarted.
- **No authentication** — MQTT broker allows anonymous connections (local dev only).
