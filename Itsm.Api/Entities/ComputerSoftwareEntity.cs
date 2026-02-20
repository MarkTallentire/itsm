namespace Itsm.Api;

public class ComputerSoftwareEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public Guid SoftwareTitleId { get; set; }
    public string Version { get; set; } = "";
    public string? InstallDate { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
    public SoftwareTitleEntity SoftwareTitle { get; set; } = null!;
}
