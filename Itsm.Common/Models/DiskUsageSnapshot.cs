namespace Itsm.Common.Models;

public record DiskUsageSnapshot(
    string ComputerName,
    DateTime ScannedAtUtc,
    long MinimumSizeBytes,
    List<DirectoryNode> Roots);
