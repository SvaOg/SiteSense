### The New Scenario: "The Autonomous Compactor"

Instead of a battery charger, we are going to simulate a **Smart Soil Compactor** on a construction site.

- **The Device:** A compactor driving over a road base.
- **The Data:** It streams **GNSS Position** (Latitude, Longitude, Elevation) and **Vibration/Compaction Value** at 50-100Hz.
- **The Challenge:** The vehicle moves fast. If we lose data, the construction crew won't know if that patch of road was compacted enough, leading to structural failure later. We must capture every packet, even if the site's cellular uplink to the cloud (SQL DB) is choppy.