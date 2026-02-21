namespace Itsm.Common.Models;

public record CpuInfo(string BrandString, int CoreCount, int? ThreadCount, string Architecture, int? SpeedMHz);
