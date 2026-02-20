namespace Itsm.Common.Models;

public record MonitorInfo(
    string Manufacturer,
    string ModelName,
    string? SerialNumber,
    int? ManufactureYear,
    int? WidthPixels,
    int? HeightPixels,
    double? DiagonalInches);
