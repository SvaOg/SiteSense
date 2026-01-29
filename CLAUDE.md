# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Interaction Mode: Guided Learning

This project is a learning exercise for interview preparation. **Do not implement code directly unless explicitly asked.** Instead:

1. **Explain what needs to be done** - Describe the next step, why it matters, and what concepts are involved
2. **Wait for the user** - They will either implement it themselves or ask you to do it
3. **Teach, don't just build** - Focus on helping the user understand the "why" behind each decision

## Development Environment

- **Language:** C#
- **IDE:** Visual Studio 2022 Community Edition
- **Target Framework:** .NET 9.0
- **NuGet Packages:** Stored locally in `./packages/` folder (not global cache)

## Project Overview

SiteSense is a high-throughput telemetry ingestion system for construction equipment. It simulates a Smart Soil Compactor that streams GNSS position (lat, lon, elevation) and vibration/compaction data at 50-100Hz. The system must capture every packet reliably even when database writes are slower than the incoming stream.

## Architecture

**Producer-Consumer pattern using MQTT + Channels:**

1. **CompactorSimulator (Edge Device/Producer)** - C# Console App that generates fake GPS and vibration data, publishes to MQTT topic `site/{siteId}/vehicle/{vehicleId}/telemetry`

2. **MQTT Broker** - Mosquitto running in Docker or locally, sits between simulator and ingestion service

3. **Ingestion Service (Consumer)** - .NET Worker Service with two layers:
   - **Layer 1 (Ingest):** Subscribes to MQTT topic via MQTTnet library, drops messages into `System.Threading.Channels.Channel`
   - **Layer 2 (Process):** Background thread reads from Channel, batches messages (e.g., 500 items), bulk-inserts to SQL using `SqlBulkCopy`

4. **Database** - SQL Server or PostgreSQL with time-series optimized table structure

## Implementation Phases

- **Phase 1:** MQTT broker setup + CompactorSimulator producing JSON data
- **Phase 2:** Worker Service connecting to MQTT, parsing incoming JSON at 100 msg/sec
- **Phase 3:** Channel-based decoupling between MQTT receiver and processor
- **Phase 4:** SQL batch writer replacing console logger, using SqlBulkCopy for high-performance persistence

## Key Libraries

- **MQTTnet** - MQTT client for .NET
- **System.Threading.Channels** - Internal buffering/decoupling
- **SqlBulkCopy** or **Dapper** - Batch database writes

## Current Status

Project is in planning phase. No code has been implemented yet.
