namespace Itsm.Api;

public class DockerContainerEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string ContainerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string State { get; set; } = "";
    public string? Status { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}
