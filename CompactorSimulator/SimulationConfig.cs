namespace CompactorSimulator;

internal class SimulationConfig
{
    public int SiteId { get; set; }
    public List<VehicleConfig> Vehicles { get; set; } = new List<VehicleConfig>();
}
