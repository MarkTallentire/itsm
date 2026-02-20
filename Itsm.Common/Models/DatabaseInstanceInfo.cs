namespace Itsm.Common.Models;

public record DatabaseInstanceInfo(string Name, string? Version, int? Port, bool IsRunning);
