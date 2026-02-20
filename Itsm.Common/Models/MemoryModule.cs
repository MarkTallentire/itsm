namespace Itsm.Common.Models;

public record MemoryModule(
    string? SlotLabel,
    long CapacityBytes,
    int? SpeedMHz,
    string? Type,
    string? Manufacturer,
    string? SerialNumber);
