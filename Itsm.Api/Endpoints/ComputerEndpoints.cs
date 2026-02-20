using Itsm.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Itsm.Api.Endpoints;

public static class ComputerEndpoints
{
    public static void MapComputerEndpoints(this WebApplication app)
    {
        app.MapPost("/inventory/computer", async (Computer computer, ItsmDbContext db) =>
        {
            var existing = await db.Computers.FindAsync(computer.Identity.ComputerName);
            if (existing is null)
            {
                db.Computers.Add(new ComputerRecord
                {
                    ComputerName = computer.Identity.ComputerName,
                    LastUpdatedUtc = DateTime.UtcNow,
                    Data = computer
                });
            }
            else
            {
                existing.Data = computer;
                existing.LastUpdatedUtc = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            return Results.Ok(computer);
        }).WithName("inventory/computer");

        app.MapGet("/inventory/computers", async (ItsmDbContext db) =>
            await db.Computers.OrderBy(c => c.ComputerName).ToListAsync());

        app.MapGet("/inventory/computers/{computerName}", async (string computerName, ItsmDbContext db) =>
        {
            var record = await db.Computers.FindAsync(computerName);
            return record is null ? Results.NotFound() : Results.Ok(record);
        });
    }
}
