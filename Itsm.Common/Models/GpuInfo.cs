namespace Itsm.Common.Models;

public record GpuInfo(string Name, string Vendor, long? VramBytes, string? DriverVersion);
