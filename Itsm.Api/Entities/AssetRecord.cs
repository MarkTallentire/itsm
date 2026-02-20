namespace Itsm.Api;

public class AssetRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "InUse";
    public string? SerialNumber { get; set; }
    public string? AssignedUser { get; set; }
    public string? Location { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }
    public string Source { get; set; } = "Agent";
    public string? DiscoveredByAgent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
