namespace Itsm.Api;

public class VpnConnectionEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string Name { get; set; } = "";
    public string? Type { get; set; }
    public string? ServerAddress { get; set; }
    public bool IsConnected { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}
