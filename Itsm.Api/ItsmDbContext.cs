using Itsm.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Itsm.Api;

// ── Asset base table ──
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

// ── Computer (1:1 with Asset) ──
public class ComputerEntity
{
    public Guid Id { get; set; }
    public string ComputerName { get; set; } = "";
    public string ModelName { get; set; } = "";
    public string HardwareUuid { get; set; } = "";
    public string LoggedInUser { get; set; } = "";
    public string ChassisType { get; set; } = "";
    public string CpuBrand { get; set; } = "";
    public int CpuCores { get; set; }
    public string CpuArchitecture { get; set; } = "";
    public long TotalMemoryBytes { get; set; }
    public string OsDescription { get; set; } = "";
    public string? OsVersion { get; set; }
    public string? OsBuildNumber { get; set; }
    public string Hostname { get; set; } = "";
    public bool FirewallEnabled { get; set; }
    public bool? FirewallStealth { get; set; }
    public bool EncryptionEnabled { get; set; }
    public string? EncryptionMethod { get; set; }
    public bool BatteryPresent { get; set; }
    public double? BatteryCharge { get; set; }
    public int? BatteryCycles { get; set; }
    public double? BatteryHealth { get; set; }
    public bool? BatteryCharging { get; set; }
    public string? BatteryCondition { get; set; }
    public DateTime? LastBootUtc { get; set; }
    public string? Uptime { get; set; }
    public DateTime LastUpdatedUtc { get; set; }

    public AssetRecord Asset { get; set; } = null!;
    public List<DiskEntity> Disks { get; set; } = [];
    public List<NetworkInterfaceEntity> NetworkInterfaces { get; set; } = [];
    public List<ComputerGpuEntity> ComputerGpus { get; set; } = [];
    public List<ComputerSoftwareEntity> ComputerSoftware { get; set; } = [];
}

public class DiskEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string Name { get; set; } = "";
    public string Format { get; set; } = "";
    public long TotalBytes { get; set; }
    public long FreeBytes { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}

public class NetworkInterfaceEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string Name { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string[] IpAddresses { get; set; } = [];

    public ComputerEntity Computer { get; set; } = null!;
}

// ── Software ──
public class SoftwareTitleEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class ComputerSoftwareEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public Guid SoftwareTitleId { get; set; }
    public string Version { get; set; } = "";
    public string? InstallDate { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
    public SoftwareTitleEntity SoftwareTitle { get; set; } = null!;
}

// ── GPU ──
public class GpuModelEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Vendor { get; set; } = "";
}

public class ComputerGpuEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public Guid GpuModelId { get; set; }
    public long? VramBytes { get; set; }
    public string? DriverVersion { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
    public GpuModelEntity GpuModel { get; set; } = null!;
}

// ── Monitor ──
public class MonitorModelEntity
{
    public Guid Id { get; set; }
    public string Manufacturer { get; set; } = "";
    public string ModelName { get; set; } = "";
    public int? WidthPixels { get; set; }
    public int? HeightPixels { get; set; }
    public double? DiagonalInches { get; set; }
}

public class MonitorEntity
{
    public Guid Id { get; set; }
    public Guid MonitorModelId { get; set; }
    public int? ManufactureYear { get; set; }

    public AssetRecord Asset { get; set; } = null!;
    public MonitorModelEntity MonitorModel { get; set; } = null!;
}

// ── USB Peripheral ──
public class UsbProductEntity
{
    public Guid Id { get; set; }
    public string VendorId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Manufacturer { get; set; }
}

public class UsbPeripheralEntity
{
    public Guid Id { get; set; }
    public Guid UsbProductId { get; set; }

    public AssetRecord Asset { get; set; } = null!;
    public UsbProductEntity UsbProduct { get; set; } = null!;
}

// ── Network Printer ──
public class PrinterModelEntity
{
    public Guid Id { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
}

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

// ── Existing (kept as-is) ──
public class DiskUsageRecord
{
    public string ComputerName { get; set; } = "";
    public DateTime ScannedAtUtc { get; set; }
    public DiskUsageSnapshot Data { get; set; } = null!;
}

public class AgentRecord
{
    public string HardwareUuid { get; set; } = "";
    public string ComputerName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string AgentVersion { get; set; } = "";
    public bool IsConnected { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public DateTime FirstSeenUtc { get; set; }
}

// ── DbContext ──
public class ItsmDbContext(DbContextOptions<ItsmDbContext> options) : DbContext(options)
{
    public DbSet<AssetRecord> Assets => Set<AssetRecord>();
    public DbSet<ComputerEntity> Computers => Set<ComputerEntity>();
    public DbSet<DiskEntity> Disks => Set<DiskEntity>();
    public DbSet<NetworkInterfaceEntity> NetworkInterfaces => Set<NetworkInterfaceEntity>();
    public DbSet<SoftwareTitleEntity> SoftwareTitles => Set<SoftwareTitleEntity>();
    public DbSet<ComputerSoftwareEntity> ComputerSoftware => Set<ComputerSoftwareEntity>();
    public DbSet<GpuModelEntity> GpuModels => Set<GpuModelEntity>();
    public DbSet<ComputerGpuEntity> ComputerGpus => Set<ComputerGpuEntity>();
    public DbSet<MonitorModelEntity> MonitorModels => Set<MonitorModelEntity>();
    public DbSet<MonitorEntity> Monitors => Set<MonitorEntity>();
    public DbSet<UsbProductEntity> UsbProducts => Set<UsbProductEntity>();
    public DbSet<UsbPeripheralEntity> UsbPeripherals => Set<UsbPeripheralEntity>();
    public DbSet<PrinterModelEntity> PrinterModels => Set<PrinterModelEntity>();
    public DbSet<NetworkPrinterEntity> NetworkPrinters => Set<NetworkPrinterEntity>();
    public DbSet<DiskUsageRecord> DiskUsageSnapshots => Set<DiskUsageRecord>();
    public DbSet<AgentRecord> Agents => Set<AgentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Assets ──
        modelBuilder.Entity<AssetRecord>(e =>
        {
            e.HasKey(a => a.Id);
        });

        // ── Computers (1:1 with Asset, shared PK) ──
        modelBuilder.Entity<ComputerEntity>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.Asset).WithOne().HasForeignKey<ComputerEntity>(c => c.Id);
            e.HasIndex(c => c.ComputerName).IsUnique();
            e.HasIndex(c => c.HardwareUuid).IsUnique();
            e.HasMany(c => c.Disks).WithOne(d => d.Computer).HasForeignKey(d => d.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.NetworkInterfaces).WithOne(n => n.Computer).HasForeignKey(n => n.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.ComputerGpus).WithOne(g => g.Computer).HasForeignKey(g => g.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.ComputerSoftware).WithOne(s => s.Computer).HasForeignKey(s => s.ComputerId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Disks ──
        modelBuilder.Entity<DiskEntity>(e =>
        {
            e.HasKey(d => d.Id);
        });

        // ── Network Interfaces ──
        modelBuilder.Entity<NetworkInterfaceEntity>(e =>
        {
            e.HasKey(n => n.Id);
        });

        // ── Software Titles ──
        modelBuilder.Entity<SoftwareTitleEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Name).IsUnique();
        });

        // ── Computer Software ──
        modelBuilder.Entity<ComputerSoftwareEntity>(e =>
        {
            e.HasKey(cs => cs.Id);
            e.HasOne(cs => cs.SoftwareTitle).WithMany().HasForeignKey(cs => cs.SoftwareTitleId);
        });

        // ── GPU Models ──
        modelBuilder.Entity<GpuModelEntity>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => new { g.Name, g.Vendor }).IsUnique();
        });

        // ── Computer GPUs ──
        modelBuilder.Entity<ComputerGpuEntity>(e =>
        {
            e.HasKey(cg => cg.Id);
            e.HasOne(cg => cg.GpuModel).WithMany().HasForeignKey(cg => cg.GpuModelId);
        });

        // ── Monitor Models ──
        modelBuilder.Entity<MonitorModelEntity>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.Manufacturer, m.ModelName }).IsUnique();
        });

        // ── Monitors (1:1 with Asset, shared PK) ──
        modelBuilder.Entity<MonitorEntity>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasOne(m => m.Asset).WithOne().HasForeignKey<MonitorEntity>(m => m.Id);
            e.HasOne(m => m.MonitorModel).WithMany().HasForeignKey(m => m.MonitorModelId);
        });

        // ── USB Products ──
        modelBuilder.Entity<UsbProductEntity>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => new { u.VendorId, u.ProductId }).IsUnique();
        });

        // ── USB Peripherals (1:1 with Asset, shared PK) ──
        modelBuilder.Entity<UsbPeripheralEntity>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasOne(u => u.Asset).WithOne().HasForeignKey<UsbPeripheralEntity>(u => u.Id);
            e.HasOne(u => u.UsbProduct).WithMany().HasForeignKey(u => u.UsbProductId);
        });

        // ── Printer Models ──
        modelBuilder.Entity<PrinterModelEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.Manufacturer, p.Model }).IsUnique();
        });

        // ── Network Printers (1:1 with Asset, shared PK) ──
        modelBuilder.Entity<NetworkPrinterEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.Asset).WithOne().HasForeignKey<NetworkPrinterEntity>(p => p.Id);
            e.HasOne(p => p.PrinterModel).WithMany().HasForeignKey(p => p.PrinterModelId);
        });

        // ── Existing (kept as-is) ──
        modelBuilder.Entity<DiskUsageRecord>(e =>
        {
            e.HasKey(d => d.ComputerName);
            e.Property(d => d.Data).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AgentRecord>(e =>
        {
            e.HasKey(a => a.HardwareUuid);
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

        if (!db.Database.IsRelational())
        {
            db.Database.EnsureCreated();
            return;
        }

        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                db.Database.Migrate();
                break;
            }
            catch (Exception ex) when (attempt < 9)
            {
                logger.LogWarning(ex, "Database migration attempt {Attempt} failed, retrying...", attempt + 1);
                Thread.Sleep(2000);
            }
        }

        try
        {
            db.Database.ExecuteSqlRaw("UPDATE \"Agents\" SET \"IsConnected\" = false");
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
        {
            // Table does not exist on first run — safe to ignore
        }
    }
}
