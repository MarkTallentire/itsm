namespace Itsm.Api;

public class DnsServerEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string Address { get; set; } = "";

    public ComputerEntity Computer { get; set; } = null!;
}
