namespace Itsm.Api;

public class ListeningPortEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public int Port { get; set; }
    public string Protocol { get; set; } = "TCP";
    public string? ProcessName { get; set; }
    public int? Pid { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}
