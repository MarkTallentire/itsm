namespace Itsm.Api;

public class ControllerEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string Name { get; set; } = "";
    public string? Manufacturer { get; set; }
    public string? Type { get; set; }
    public string? PciId { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}
