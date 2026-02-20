using Microsoft.EntityFrameworkCore;

namespace Itsm.Api.Endpoints;

public static class AssetEndpoints
{
    public static void MapAssetEndpoints(this WebApplication app)
    {
        app.MapGet("/assets", async (string? type, string? status, string? search, ItsmDbContext db) =>
        {
            var query = db.Assets.AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.Type == type);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            if (!string.IsNullOrEmpty(search))
            {
                var term = search.ToLower();
                query = query.Where(a =>
                    a.Name.ToLower().Contains(term) ||
                    (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(term)) ||
                    (a.AssignedUser != null && a.AssignedUser.ToLower().Contains(term)));
            }

            var assets = await query.OrderBy(a => a.Name).ToListAsync();

            var agentUuids = assets
                .Where(a => a.DiscoveredByAgent != null)
                .Select(a => a.DiscoveredByAgent!)
                .Distinct()
                .ToList();

            var uuidToName = await db.Computers
                .Where(c => agentUuids.Contains(c.HardwareUuid))
                .ToDictionaryAsync(c => c.HardwareUuid, c => c.ComputerName);

            return assets.Select(a => new
            {
                a.Id, a.Name, a.Type, a.Status, a.SerialNumber, a.AssignedUser,
                a.Location, a.PurchaseDate, a.WarrantyExpiry, a.Cost, a.Notes,
                a.Source, a.DiscoveredByAgent,
                DiscoveredByComputerName = a.DiscoveredByAgent != null && uuidToName.TryGetValue(a.DiscoveredByAgent, out var cn) ? cn : null,
                a.CreatedAtUtc, a.UpdatedAtUtc,
            }).ToList();
        });

        app.MapGet("/assets/{id:guid}", async (Guid id, ItsmDbContext db) =>
        {
            var asset = await db.Assets.FindAsync(id);
            if (asset is null) return Results.NotFound();

            string? discoveredByName = null;
            if (asset.DiscoveredByAgent != null)
                discoveredByName = await db.Computers
                    .Where(c => c.HardwareUuid == asset.DiscoveredByAgent)
                    .Select(c => c.ComputerName)
                    .FirstOrDefaultAsync();

            return Results.Ok(new
            {
                asset.Id, asset.Name, asset.Type, asset.Status, asset.SerialNumber, asset.AssignedUser,
                asset.Location, asset.PurchaseDate, asset.WarrantyExpiry, asset.Cost, asset.Notes,
                asset.Source, asset.DiscoveredByAgent,
                DiscoveredByComputerName = discoveredByName,
                asset.CreatedAtUtc, asset.UpdatedAtUtc,
            });
        });

        app.MapGet("/assets/{id:guid}/printer", async (Guid id, ItsmDbContext db) =>
        {
            var printer = await db.NetworkPrinters
                .Include(p => p.Asset)
                .Include(p => p.PrinterModel)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (printer is null) return Results.NotFound();

            string? discoveredByName = null;
            if (printer.Asset.DiscoveredByAgent != null)
                discoveredByName = await db.Computers
                    .Where(c => c.HardwareUuid == printer.Asset.DiscoveredByAgent)
                    .Select(c => c.ComputerName)
                    .FirstOrDefaultAsync();

            return Results.Ok(new
            {
                printer.Asset.Id,
                printer.Asset.Name,
                printer.Asset.Status,
                printer.Asset.SerialNumber,
                printer.Asset.AssignedUser,
                printer.Asset.Location,
                printer.Asset.PurchaseDate,
                printer.Asset.WarrantyExpiry,
                printer.Asset.Cost,
                printer.Asset.Notes,
                printer.Asset.Source,
                printer.Asset.DiscoveredByAgent,
                DiscoveredByComputerName = discoveredByName,
                printer.Asset.CreatedAtUtc,
                printer.Asset.UpdatedAtUtc,
                printer.IpAddress,
                printer.MacAddress,
                printer.FirmwareVersion,
                printer.PageCount,
                printer.TonerBlackPercent,
                printer.TonerCyanPercent,
                printer.TonerMagentaPercent,
                printer.TonerYellowPercent,
                PrinterStatus = printer.Status,
                Manufacturer = printer.PrinterModel?.Manufacturer,
                Model = printer.PrinterModel?.Model,
            });
        });

        app.MapPost("/assets", async (AssetRecord asset, ItsmDbContext db) =>
        {
            var now = DateTime.UtcNow;
            asset.Id = Guid.NewGuid();
            asset.Source = "Manual";
            asset.CreatedAtUtc = now;
            asset.UpdatedAtUtc = now;
            db.Assets.Add(asset);
            await db.SaveChangesAsync();
            return Results.Created($"/assets/{asset.Id}", asset);
        });

        app.MapPut("/assets/{id:guid}", async (Guid id, AssetRecord updated, ItsmDbContext db) =>
        {
            var asset = await db.Assets.FindAsync(id);
            if (asset is null) return Results.NotFound();

            asset.Name = updated.Name;
            asset.Status = updated.Status;
            asset.AssignedUser = updated.AssignedUser;
            asset.Location = updated.Location;
            asset.PurchaseDate = updated.PurchaseDate;
            asset.WarrantyExpiry = updated.WarrantyExpiry;
            asset.Cost = updated.Cost;
            asset.Notes = updated.Notes;
            asset.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(asset);
        });

        app.MapDelete("/assets/{id:guid}", async (Guid id, ItsmDbContext db) =>
        {
            var asset = await db.Assets.FindAsync(id);
            if (asset is null) return Results.NotFound();

            db.Assets.Remove(asset);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
