namespace CompactorSimulator;

internal class VehicleConfig
{
    public string VehicleId { get; set; } = string.Empty;
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public int PublishRateHz { get; set; }
}
