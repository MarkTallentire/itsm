namespace Itsm.Common.Models;

public record SystemController(
    string Name,
    string? Manufacturer,
    string? Type,
    string? PciId);
