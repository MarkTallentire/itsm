namespace Itsm.Api;

public class DnsSearchDomainEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string Domain { get; set; } = "";

    public ComputerEntity Computer { get; set; } = null!;
}
