## Phase 4: SQL Batch Writer

Replace the placeholder processing logic in TelemetryProcessorService with SqlBulkCopy batch writes. The processor will accumulate messages into batches (e.g., 500 items) and bulk-insert them into SQL Server.

### Key concept
Instead of inserting one row per message (150 INSERT statements/sec), batch them up and write 500 rows at once using SqlBulkCopy. This turns hundreds of round-trips into one high-performance bulk operation.

### Why this matters
- Individual INSERTs at 150/sec will bottleneck on network round-trips and transaction overhead
- SqlBulkCopy uses the same protocol as BCP (bulk copy program) — optimized for high-volume writes
- Batching also lets you tune the tradeoff: bigger batches = higher throughput, but more latency before data hits the DB
