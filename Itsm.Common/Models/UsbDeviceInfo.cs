namespace Itsm.Common.Models;

public record UsbDeviceInfo(
    string VendorId,
    string ProductId,
    string Name,
    string? Manufacturer,
    string? SerialNumber);
