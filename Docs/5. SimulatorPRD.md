# CompactorSimulator - Product Requirements Document

## Overview

A configurable simulator that mimics one or more soil compactors on a construction site, publishing telemetry data over MQTT. Used for development and testing of the ingestion service.

---

## Goals

1. Generate realistic telemetry data at high frequency (50-100 Hz per device)
2. Support multiple concurrent simulated vehicles
3. Configurable without code changes
4. Easy to start/stop individual devices or the entire fleet

---

## Telemetry Data Model

Each telemetry point represents a single reading from a compactor:

| Field | Type | Description |
|-------|------|-------------|
| `Timestamp` | DateTime (UTC) | When the reading was taken |
| `VehicleId` | string | Unique identifier (e.g., "compactor-01") |
| `SiteId` | int | Construction site identifier |
| `Latitude` | double | GPS latitude in decimal degrees |
| `Longitude` | double | GPS longitude in decimal degrees |
| `Elevation` | double | Height above sea level in meters |
| `VibrationFrequency` | double | Drum vibration frequency in Hz (typically 25-45 Hz) |
| `CompactionValue` | double | Measured compaction percentage (0-100%) |
| `Speed` | double | Vehicle speed in km/h |

---

## MQTT Topic Structure

```
site/{siteId}/vehicle/{vehicleId}/telemetry
```

**Examples:**
- `site/1/vehicle/compactor-01/telemetry`
- `site/1/vehicle/compactor-02/telemetry`
- `site/2/vehicle/compactor-01/telemetry`

This hierarchy allows subscribers to:
- Listen to one vehicle: `site/1/vehicle/compactor-01/telemetry`
- Listen to all vehicles on a site: `site/1/vehicle/+/telemetry`
- Listen to everything: `site/+/vehicle/+/telemetry`

---

## Configuration

Via `appsettings.json`:

```json
{
  "Mqtt": {
    "Host": "localhost",
    "Port": 1883
  },
  "Simulation": {
    "SiteId": 1,
    "Vehicles": [
      {
        "VehicleId": "compactor-01",
        "StartLatitude": 47.6062,
        "StartLongitude": -122.3321,
        "PublishRateHz": 50
      },
      {
        "VehicleId": "compactor-02",
        "StartLatitude": 47.6065,
        "StartLongitude": -122.3325,
        "PublishRateHz": 100
      }
    ]
  }
}
```

---

## Simulation Behavior

### Movement Pattern
- Each vehicle moves in a back-and-forth pattern (simulating compaction passes)
- Speed varies: 2-8 km/h (realistic for compaction work)
- Small random variations in path to simulate real driving

### Vibration & Compaction
- Vibration frequency: 25-45 Hz range
- Compaction value: starts low (~30%), gradually increases with each pass over the same area
- Random noise added to simulate sensor readings

### Timing
- Each vehicle publishes independently on its own timer
- Timestamps are actual UTC time (not simulated)

---

## Architecture

```
┌─────────────────────────────────────────────┐
│           CompactorSimulator                │
│                                             │
│  ┌─────────────┐  ┌─────────────┐          │
│  │  Vehicle 1  │  │  Vehicle 2  │  ...     │
│  │   Task      │  │   Task      │          │
│  └──────┬──────┘  └──────┬──────┘          │
│         │                │                  │
│         └───────┬────────┘                  │
│                 ▼                           │
│         ┌─────────────┐                     │
│         │ MQTT Client │                     │
│         │  (shared)   │                     │
│         └──────┬──────┘                     │
└────────────────┼────────────────────────────┘
                 │
                 ▼
          ┌─────────────┐
          │  Mosquitto  │
          │   Broker    │
          └─────────────┘
```

- One MQTT client connection shared by all vehicles
- Each vehicle runs as a separate async Task
- Graceful shutdown stops all vehicles cleanly

---

## Command Line Interface

```
CompactorSimulator.exe [options]

Options:
  --config <path>     Path to config file (default: appsettings.json)
  --vehicle <id>      Run only this vehicle (for debugging)
  --duration <sec>    Run for N seconds then exit (default: infinite)
```

---

## Success Metrics

- Can sustain configured publish rate without drift
- Memory usage stays flat over time (no leaks)
- Clean shutdown completes within 2 seconds
- All vehicles maintain independent timing

---

## Out of Scope (for now)

- Replaying recorded real-world data
- GPS coordinate validation
- Network failure simulation
- Web UI for controlling simulation
