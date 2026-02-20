namespace Itsm.Api;

public class MemoryModuleEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string? SlotLabel { get; set; }
    public long CapacityBytes { get; set; }
    public int? SpeedMHz { get; set; }
    public string? Type { get; set; }
    public string? Manufacturer { get; set; }
    public string? SerialNumber { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}
