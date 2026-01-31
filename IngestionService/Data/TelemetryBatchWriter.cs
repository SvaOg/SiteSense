using System.Data;
using Microsoft.Data.SqlClient;
using SiteSense.Shared.Models;

namespace IngestionService.Data;

public class TelemetryBatchWriter
{
    private readonly string _connectionString;

    public TelemetryBatchWriter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task WriteBatchAsync(IReadOnlyList<TelemetryPoint> batch, CancellationToken token = default)
    {
        if (batch == null || batch.Count == 0) return;

        // 1. Build the DataTable with types matching InitDatabase.sql
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Timestamp", typeof(DateTime));
        dataTable.Columns.Add("VehicleId", typeof(string));
        dataTable.Columns.Add("Latitude", typeof(double));
        dataTable.Columns.Add("Longitude", typeof(double));
        dataTable.Columns.Add("Elevation", typeof(double));
        dataTable.Columns.Add("VibrationFrequency", typeof(double));
        dataTable.Columns.Add("CompactionValue", typeof(double));

        // 2. Loop through the batch and add rows
        foreach (var point in batch)
        {
            token.ThrowIfCancellationRequested();

            dataTable.Rows.Add(
                point.Timestamp,
                point.VehicleId,
                point.Latitude,
                point.Longitude,
                point.Elevation,
                point.VibrationFrequency,
                point.CompactionValue
            );
        }

        // 3. Setup the Connection and Bulk Copy
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(token);

        using var bulkCopy = new SqlBulkCopy(connection);
        bulkCopy.DestinationTableName = "TelemetryPoints";

        // 4. Explicit Column Mapping (Source DataTable -> Destination SQL Table)
        bulkCopy.ColumnMappings.Add("Timestamp", "Timestamp");
        bulkCopy.ColumnMappings.Add("VehicleId", "VehicleId");
        bulkCopy.ColumnMappings.Add("Latitude", "Latitude");
        bulkCopy.ColumnMappings.Add("Longitude", "Longitude");
        bulkCopy.ColumnMappings.Add("Elevation", "Elevation");
        bulkCopy.ColumnMappings.Add("VibrationFrequency", "VibrationFrequency");
        bulkCopy.ColumnMappings.Add("CompactionValue", "CompactionValue");

        // Id and IngestedAt are handled by SQL Server automatically
        await bulkCopy.WriteToServerAsync(dataTable, token);
    }
}