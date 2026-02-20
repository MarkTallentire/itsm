namespace Itsm.Api;

public class AntivirusEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string Name { get; set; } = "";
    public string? Version { get; set; }
    public bool IsEnabled { get; set; }
    public bool? IsUpToDate { get; set; }
    public string? ExpirationDate { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}
