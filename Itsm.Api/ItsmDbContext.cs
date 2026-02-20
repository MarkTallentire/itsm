using Itsm.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Itsm.Api;

public class ComputerRecord
{
    public string ComputerName { get; set; } = "";
    public DateTime LastUpdatedUtc { get; set; }
    public Computer Data { get; set; } = null!;
}

public class DiskUsageRecord
{
    public string ComputerName { get; set; } = "";
    public DateTime ScannedAtUtc { get; set; }
    public DiskUsageSnapshot Data { get; set; } = null!;
}

public class ItsmDbContext(DbContextOptions<ItsmDbContext> options) : DbContext(options)
{
    public DbSet<ComputerRecord> Computers => Set<ComputerRecord>();
    public DbSet<DiskUsageRecord> DiskUsageSnapshots => Set<DiskUsageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComputerRecord>(e =>
        {
            e.HasKey(c => c.ComputerName);
            e.Property(c => c.Data).HasColumnType("jsonb");
        });

        modelBuilder.Entity<DiskUsageRecord>(e =>
        {
            e.HasKey(d => d.ComputerName);
            e.Property(d => d.Data).HasColumnType("jsonb");
        });
    }
}

public static class MigrationExtensions
{
    public static void MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ItsmDbContext>>();

        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                db.Database.Migrate();
                return;
            }
            catch (Exception ex) when (attempt < 9)
            {
                logger.LogWarning(ex, "Database migration attempt {Attempt} failed, retrying...", attempt + 1);
                Thread.Sleep(2000);
            }
        }
    }
}
