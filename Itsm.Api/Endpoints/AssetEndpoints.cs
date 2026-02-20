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

            return await query.OrderBy(a => a.Name).ToListAsync();
        });

        app.MapGet("/assets/{id:guid}", async (Guid id, ItsmDbContext db) =>
        {
            var asset = await db.Assets.FindAsync(id);
            return asset is null ? Results.NotFound() : Results.Ok(asset);
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
