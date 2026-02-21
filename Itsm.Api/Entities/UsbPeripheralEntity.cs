namespace Itsm.Api;

public class UsbPeripheralEntity
{
    public Guid Id { get; set; }
    public Guid UsbProductId { get; set; }

    public AssetRecord Asset { get; set; } = null!;
    public UsbProductEntity UsbProduct { get; set; } = null!;
}
