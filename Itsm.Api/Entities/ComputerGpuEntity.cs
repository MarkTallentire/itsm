namespace Itsm.Api;

public class ComputerGpuEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public Guid GpuModelId { get; set; }
    public long? VramBytes { get; set; }
    public string? DriverVersion { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
    public GpuModelEntity GpuModel { get; set; } = null!;
}
