namespace Itsm.Common.Models;

public record NetworkPrinterInfo(
    string IpAddress,
    string? MacAddress,
    string? Manufacturer,
    string? Model,
    string? SerialNumber,
    string? FirmwareVersion,
    int? PageCount,
    int? TonerBlackPercent,
    int? TonerCyanPercent,
    int? TonerMagentaPercent,
    int? TonerYellowPercent,
    string? Status);
