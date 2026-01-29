namespace CompactorSimulator;

public class TelemetryPoint
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string VehicleId { get; set; } = string.Empty;
    public int SiteId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Elevation { get; set; }
    public double VibrationFrequency { get; set; }
    public double CompactionValue { get; set; }
    public double Speed { get; set; }
}
