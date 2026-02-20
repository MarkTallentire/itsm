namespace Itsm.Api;

public class DiskEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string Name { get; set; } = "";
    public string Format { get; set; } = "";
    public long TotalBytes { get; set; }
    public long FreeBytes { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}
