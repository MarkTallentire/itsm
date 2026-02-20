namespace Itsm.Api;

public class NetworkDriveEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string LocalPath { get; set; } = "";
    public string RemotePath { get; set; } = "";
    public string? FileSystem { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}
