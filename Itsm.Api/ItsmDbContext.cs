using Microsoft.EntityFrameworkCore;

namespace Itsm.Api;

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
    public DbSet<MemoryModuleEntity> MemoryModules => Set<MemoryModuleEntity>();
    public DbSet<AntivirusEntity> AntivirusProducts => Set<AntivirusEntity>();
    public DbSet<ControllerEntity> Controllers => Set<ControllerEntity>();
    public DbSet<VmInstanceEntity> VirtualMachines => Set<VmInstanceEntity>();
    public DbSet<DockerContainerEntity> DockerContainers => Set<DockerContainerEntity>();
    public DbSet<DatabaseInstanceEntity> DatabaseInstances => Set<DatabaseInstanceEntity>();
    public DbSet<VpnConnectionEntity> VpnConnections => Set<VpnConnectionEntity>();
    public DbSet<NetworkDriveEntity> NetworkDrives => Set<NetworkDriveEntity>();
    public DbSet<ListeningPortEntity> ListeningPorts => Set<ListeningPortEntity>();
    public DbSet<DnsServerEntity> DnsServers => Set<DnsServerEntity>();
    public DbSet<DnsSearchDomainEntity> DnsSearchDomains => Set<DnsSearchDomainEntity>();
    public DbSet<DiskUsageRecord> DiskUsageSnapshots => Set<DiskUsageRecord>();
    public DbSet<AgentRecord> Agents => Set<AgentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssetRecord>(e =>
        {
            e.HasKey(a => a.Id);
        });

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
            e.HasMany(c => c.MemoryModules).WithOne(m => m.Computer).HasForeignKey(m => m.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.AntivirusProducts).WithOne(a => a.Computer).HasForeignKey(a => a.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Controllers).WithOne(ct => ct.Computer).HasForeignKey(ct => ct.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.VirtualMachines).WithOne(v => v.Computer).HasForeignKey(v => v.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.DockerContainers).WithOne(d => d.Computer).HasForeignKey(d => d.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.DatabaseInstances).WithOne(d => d.Computer).HasForeignKey(d => d.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.VpnConnections).WithOne(v => v.Computer).HasForeignKey(v => v.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.NetworkDrives).WithOne(n => n.Computer).HasForeignKey(n => n.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.ListeningPorts).WithOne(l => l.Computer).HasForeignKey(l => l.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.DnsServers).WithOne(d => d.Computer).HasForeignKey(d => d.ComputerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.DnsSearchDomains).WithOne(d => d.Computer).HasForeignKey(d => d.ComputerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DiskEntity>(e =>
        {
            e.HasKey(d => d.Id);
        });

        modelBuilder.Entity<NetworkInterfaceEntity>(e =>
        {
            e.HasKey(n => n.Id);
        });

        modelBuilder.Entity<SoftwareTitleEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Name).IsUnique();
        });

        modelBuilder.Entity<ComputerSoftwareEntity>(e =>
        {
            e.HasKey(cs => cs.Id);
            e.HasOne(cs => cs.SoftwareTitle).WithMany().HasForeignKey(cs => cs.SoftwareTitleId);
        });

        modelBuilder.Entity<GpuModelEntity>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => new { g.Name, g.Vendor }).IsUnique();
        });

        modelBuilder.Entity<ComputerGpuEntity>(e =>
        {
            e.HasKey(cg => cg.Id);
            e.HasOne(cg => cg.GpuModel).WithMany().HasForeignKey(cg => cg.GpuModelId);
        });

        modelBuilder.Entity<MonitorModelEntity>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.Manufacturer, m.ModelName }).IsUnique();
        });

        modelBuilder.Entity<MonitorEntity>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasOne(m => m.Asset).WithOne().HasForeignKey<MonitorEntity>(m => m.Id);
            e.HasOne(m => m.MonitorModel).WithMany().HasForeignKey(m => m.MonitorModelId);
        });

        modelBuilder.Entity<UsbProductEntity>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => new { u.VendorId, u.ProductId }).IsUnique();
        });

        modelBuilder.Entity<UsbPeripheralEntity>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasOne(u => u.Asset).WithOne().HasForeignKey<UsbPeripheralEntity>(u => u.Id);
            e.HasOne(u => u.UsbProduct).WithMany().HasForeignKey(u => u.UsbProductId);
        });

        modelBuilder.Entity<PrinterModelEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.Manufacturer, p.Model }).IsUnique();
        });

        modelBuilder.Entity<NetworkPrinterEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.Asset).WithOne().HasForeignKey<NetworkPrinterEntity>(p => p.Id);
            e.HasOne(p => p.PrinterModel).WithMany().HasForeignKey(p => p.PrinterModelId);
        });

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
            // Table does not exist on first run
        }
    }
}
