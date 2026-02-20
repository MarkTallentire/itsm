namespace Itsm.Api;

public class VmInstanceEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string Name { get; set; } = "";
    public string? State { get; set; }
    public string? Type { get; set; }
    public long? MemoryMB { get; set; }
    public int? CpuCount { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}
