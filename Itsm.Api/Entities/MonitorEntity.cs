namespace Itsm.Api;

public class MonitorEntity
{
    public Guid Id { get; set; }
    public Guid MonitorModelId { get; set; }
    public int? ManufactureYear { get; set; }

    public AssetRecord Asset { get; set; } = null!;
    public MonitorModelEntity MonitorModel { get; set; } = null!;
}
