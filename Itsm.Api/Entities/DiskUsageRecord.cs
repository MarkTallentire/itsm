using Itsm.Common.Models;

namespace Itsm.Api;

public class DiskUsageRecord
{
    public string ComputerName { get; set; } = "";
    public DateTime ScannedAtUtc { get; set; }
    public DiskUsageSnapshot Data { get; set; } = null!;
}
