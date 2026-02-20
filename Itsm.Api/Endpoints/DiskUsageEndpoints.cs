using Itsm.Common.Models;

namespace Itsm.Api.Endpoints;

public static class DiskUsageEndpoints
{
    public static void MapDiskUsageEndpoints(this WebApplication app)
    {
        app.MapPost("/inventory/disk-usage", async (DiskUsageSnapshot snapshot, ItsmDbContext db) =>
        {
            var existing = await db.DiskUsageSnapshots.FindAsync(snapshot.ComputerName);
            if (existing is null)
            {
                db.DiskUsageSnapshots.Add(new DiskUsageRecord
                {
                    ComputerName = snapshot.ComputerName,
                    ScannedAtUtc = snapshot.ScannedAtUtc,
                    Data = snapshot
                });
            }
            else
            {
                existing.Data = snapshot;
                existing.ScannedAtUtc = snapshot.ScannedAtUtc;
            }

            await db.SaveChangesAsync();
            return Results.Ok(snapshot);
        }).WithName("inventory/disk-usage");

        app.MapGet("/inventory/disk-usage/{computerName}", async (string computerName, ItsmDbContext db) =>
        {
            var record = await db.DiskUsageSnapshots.FindAsync(computerName);
            return record is null ? Results.NotFound() : Results.Ok(record);
        });
    }
}
