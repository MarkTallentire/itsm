namespace Itsm.Common.Models;

public record MemoryInfo(long TotalBytes, List<MemoryModule> Modules);
