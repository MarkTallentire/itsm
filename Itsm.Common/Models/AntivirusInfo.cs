namespace Itsm.Common.Models;

public record AntivirusInfo(string Name, string? Version, bool IsEnabled, bool? IsUpToDate, string? ExpirationDate);
