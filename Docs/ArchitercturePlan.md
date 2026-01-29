### The Architecture Plan

We will build a **Producer-Consumer** system using **MQTT** for the transport and **Channels** for internal buffering.

**1. The "Edge" Device (Producer)**

- A simple C# Console App that acts as the simulator.
- It generates fake GPS and vibration data.
- It publishes to an MQTT topic (e.g., `site/42/vehicle/compactor-01/telemetry`) at high frequency.

**2. The Broker (The Postman)**

- We need an MQTT Broker to sit between the device and your service.
- *Choice:* We can run **Mosquitto** (standard, lightweight) in a Docker container or just install it locally.

**3. The Ingestion Service (The Consumer)**

- This is the core of your interview prep. A .NET Worker Service.
- **Layer 1 (Ingest):** Subscribes to the MQTT topic. Receives messages and immediately drops them into a `System.Threading.Channels.Channel`.
- **Layer 2 (Process):** A background thread reads from the Channel, groups messages into batches (e.g., 500 items), and bulk-inserts them into SQL.

**4. The Database**

- SQL Server (or PostgreSQL). We will use a simple table structure designed for time-series data.