namespace Itsm.Api;

public class NetworkPrinterEntity
{
    public Guid Id { get; set; }
    public Guid? PrinterModelId { get; set; }
    public string IpAddress { get; set; } = "";
    public string? MacAddress { get; set; }
    public string? FirmwareVersion { get; set; }
    public int? PageCount { get; set; }
    public int? TonerBlackPercent { get; set; }
    public int? TonerCyanPercent { get; set; }
    public int? TonerMagentaPercent { get; set; }
    public int? TonerYellowPercent { get; set; }
    public string? Status { get; set; }

    public AssetRecord Asset { get; set; } = null!;
    public PrinterModelEntity? PrinterModel { get; set; }
}
