namespace Itsm.Api;

public class MonitorModelEntity
{
    public Guid Id { get; set; }
    public string Manufacturer { get; set; } = "";
    public string ModelName { get; set; } = "";
    public int? WidthPixels { get; set; }
    public int? HeightPixels { get; set; }
    public double? DiagonalInches { get; set; }
}
