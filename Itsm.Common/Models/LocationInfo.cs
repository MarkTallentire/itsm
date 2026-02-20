namespace Itsm.Common.Models;

public record LocationInfo(
    double? Latitude,
    double? Longitude,
    string? City,
    string? Region,
    string? Country,
    string? Timezone,
    string? PublicIp);
