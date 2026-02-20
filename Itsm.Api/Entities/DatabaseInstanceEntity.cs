namespace Itsm.Api;

public class DatabaseInstanceEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string Name { get; set; } = "";
    public string? Version { get; set; }
    public int? Port { get; set; }
    public bool IsRunning { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}
