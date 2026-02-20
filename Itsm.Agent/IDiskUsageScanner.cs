using Itsm.Common.Models;

namespace Itsm.Agent;

public interface IDiskUsageScanner
{
    DiskUsageSnapshot Scan(long minimumSizeBytes);
}
