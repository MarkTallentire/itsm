namespace Itsm.Api;

public class UsbProductEntity
{
    public Guid Id { get; set; }
    public string VendorId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Manufacturer { get; set; }
}
