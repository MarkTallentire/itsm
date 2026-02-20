using System.Runtime.InteropServices;
using Itsm.Common.Models;

namespace Itsm.Agent;

public class DiskUsageScanner(ILogger<DiskUsageScanner> logger) : IDiskUsageScanner
{
    private static readonly HashSet<string> SkipPaths = GetSkipPaths();
    private int _directoriesScanned;
    private string _currentPath = "";

    public DiskUsageSnapshot Scan(long minimumSizeBytes)
    {
        var roots = new List<DirectoryNode>();
        _directoriesScanned = 0;

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
                continue;

            var rootPath = drive.RootDirectory.FullName;

            if (SkipPaths.Contains(rootPath.TrimEnd(Path.DirectorySeparatorChar)))
                continue;

            logger.LogInformation("Scanning drive {RootPath}", rootPath);
            var node = ScanDirectory(new DirectoryInfo(rootPath), minimumSizeBytes);
            roots.Add(node);
            logger.LogInformation("Finished drive {RootPath} — {Size:N0} bytes across {Count:N0} directories",
                rootPath, node.SizeBytes, _directoriesScanned);
        }

        return new DiskUsageSnapshot(
            ComputerName: Environment.MachineName,
            ScannedAtUtc: DateTime.UtcNow,
            MinimumSizeBytes: minimumSizeBytes,
            Roots: roots);
    }

    private DirectoryNode ScanDirectory(DirectoryInfo directory, long minimumSizeBytes)
    {
        _directoriesScanned++;
        _currentPath = directory.FullName;

        if (_directoriesScanned % 1000 == 0)
        {
            logger.LogInformation("Progress: {Count:N0} directories scanned — currently at {Path}", _directoriesScanned, _currentPath);
            Thread.Sleep(10); // yield CPU so other workers can make progress
        }

        long totalSize = 0;
        var children = new List<DirectoryNode>();

        try
        {
            foreach (var file in directory.EnumerateFiles())
            {
                try
                {
                    totalSize += file.Length;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    // Skip inaccessible files
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Skip inaccessible directory contents
        }

        try
        {
            foreach (var subDir in directory.EnumerateDirectories())
            {
                var subPath = subDir.FullName.TrimEnd(Path.DirectorySeparatorChar);
                if (SkipPaths.Contains(subPath))
                    continue;

                try
                {
                    var childNode = ScanDirectory(subDir, minimumSizeBytes);
                    totalSize += childNode.SizeBytes;

                    if (childNode.SizeBytes >= minimumSizeBytes)
                        children.Add(childNode);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    logger.LogDebug("Skipping inaccessible directory: {Path} — {Message}", subDir.FullName, ex.Message);
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Skip inaccessible directory listing
        }

        return new DirectoryNode(directory.FullName, totalSize, children);
    }

    private static HashSet<string> GetSkipPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new HashSet<string>(StringComparer.Ordinal) { "/proc", "/sys", "/dev" };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new HashSet<string>(StringComparer.Ordinal)
            {
                "/dev",
                "/System",
                "/Volumes",
                "/private/var/vm",
                "/private/var/folders",
                "/private/var/db/dyld",
                "/Library/Caches",
                "/System/Volumes",
            };

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
