namespace Itsm.Common.Models;

public record OsInfo(
    string Description,
    string? Version,
    string? BuildNumber,
    string? KernelName,
    string? KernelVersion,
    string? Architecture,
    string? InstallDate,
    string? LicenseKey);
