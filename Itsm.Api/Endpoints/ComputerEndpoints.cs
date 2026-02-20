using Itsm.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Itsm.Api.Endpoints;

public static class ComputerEndpoints
{
    public static void MapComputerEndpoints(this WebApplication app)
    {
        app.MapPost("/inventory/computer", async (Computer computer, ItsmDbContext db) =>
        {
            var now = DateTime.UtcNow;
            var name = computer.Identity.ComputerName;

            // Find existing computer by ComputerName
            var existing = await db.Computers
                .Include(c => c.Disks)
                .Include(c => c.NetworkInterfaces)
                .Include(c => c.ComputerGpus)
                .Include(c => c.ComputerSoftware)
                .Include(c => c.Asset)
                .FirstOrDefaultAsync(c => c.ComputerName == name);

            ComputerEntity comp;
            if (existing is null)
            {
                var asset = new AssetRecord
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Type = nameof(AssetType.Computer),
                    Status = nameof(AssetStatus.InUse),
                    SerialNumber = computer.Identity.SerialNumber,
                    Source = nameof(DiscoverySource.Agent),
                    DiscoveredByAgent = computer.Identity.HardwareUuid,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                db.Assets.Add(asset);

                comp = new ComputerEntity { Id = asset.Id, Asset = asset };
                db.Computers.Add(comp);
            }
            else
            {
                comp = existing;
                comp.Asset.SerialNumber = computer.Identity.SerialNumber;
                comp.Asset.DiscoveredByAgent = computer.Identity.HardwareUuid;
                comp.Asset.UpdatedAtUtc = now;

                // Remove old child rows
                db.Disks.RemoveRange(comp.Disks);
                db.NetworkInterfaces.RemoveRange(comp.NetworkInterfaces);
                db.ComputerGpus.RemoveRange(comp.ComputerGpus);
                db.ComputerSoftware.RemoveRange(comp.ComputerSoftware);
            }

            // Map scalar fields
            comp.ComputerName = name;
            comp.ModelName = computer.Identity.ModelName;
            comp.HardwareUuid = computer.Identity.HardwareUuid;
            comp.LoggedInUser = computer.Identity.LoggedInUser;
            comp.ChassisType = computer.Identity.ChassisType.ToString();
            comp.CpuBrand = computer.Cpu.BrandString;
            comp.CpuCores = computer.Cpu.CoreCount;
            comp.CpuArchitecture = computer.Cpu.Architecture;
            comp.TotalMemoryBytes = computer.Memory.TotalBytes;
            comp.OsDescription = computer.Os.Description;
            comp.OsVersion = computer.Os.Version;
            comp.OsBuildNumber = computer.Os.BuildNumber;
            comp.Hostname = computer.Network.Hostname;
            comp.FirewallEnabled = computer.Firewall.IsEnabled;
            comp.FirewallStealth = computer.Firewall.StealthMode;
            comp.EncryptionEnabled = computer.Encryption.IsEnabled;
            comp.EncryptionMethod = computer.Encryption.Method;
            comp.BatteryPresent = computer.Battery.IsPresent;
            comp.BatteryCharge = computer.Battery.ChargePercent;
            comp.BatteryCycles = computer.Battery.CycleCount;
            comp.BatteryHealth = computer.Battery.HealthPercent;
            comp.BatteryCharging = computer.Battery.IsCharging;
            comp.BatteryCondition = computer.Battery.Condition;
            comp.LastBootUtc = computer.Uptime.LastBootUtc;
            comp.Uptime = computer.Uptime.Uptime.ToString();
            comp.LastUpdatedUtc = now;

            // Disks
            foreach (var d in computer.Disks)
            {
                db.Disks.Add(new DiskEntity
                {
                    Id = Guid.NewGuid(),
                    ComputerId = comp.Id,
                    Name = d.Name,
                    Format = d.Format,
                    TotalBytes = d.TotalBytes,
                    FreeBytes = d.FreeBytes
                });
            }

            // Network interfaces
            foreach (var iface in computer.Network.Interfaces)
            {
                db.NetworkInterfaces.Add(new NetworkInterfaceEntity
                {
                    Id = Guid.NewGuid(),
                    ComputerId = comp.Id,
                    Name = iface.Name,
                    MacAddress = iface.MacAddress,
                    IpAddresses = iface.IpAddresses.ToArray()
                });
            }

            // GPUs — find-or-create model, then add per-machine row
            foreach (var gpu in computer.Gpus)
            {
                var model = await db.GpuModels
                    .FirstOrDefaultAsync(g => g.Name == gpu.Name && g.Vendor == gpu.Vendor)
                    ?? db.GpuModels.Local.FirstOrDefault(g => g.Name == gpu.Name && g.Vendor == gpu.Vendor);
                if (model is null)
                {
                    model = new GpuModelEntity
                    {
                        Id = Guid.NewGuid(),
                        Name = gpu.Name,
                        Vendor = gpu.Vendor
                    };
                    db.GpuModels.Add(model);
                }

                db.ComputerGpus.Add(new ComputerGpuEntity
                {
                    Id = Guid.NewGuid(),
                    ComputerId = comp.Id,
                    GpuModelId = model.Id,
                    VramBytes = gpu.VramBytes,
                    DriverVersion = gpu.DriverVersion
                });
            }

            // Installed software — find-or-create title, then add per-machine row
            foreach (var appInfo in computer.InstalledApps)
            {
                var title = await db.SoftwareTitles
                    .FirstOrDefaultAsync(s => s.Name == appInfo.Name)
                    ?? db.SoftwareTitles.Local.FirstOrDefault(s => s.Name == appInfo.Name);
                if (title is null)
                {
                    title = new SoftwareTitleEntity
                    {
                        Id = Guid.NewGuid(),
                        Name = appInfo.Name
                    };
                    db.SoftwareTitles.Add(title);
                }

                db.ComputerSoftware.Add(new ComputerSoftwareEntity
                {
                    Id = Guid.NewGuid(),
                    ComputerId = comp.Id,
                    SoftwareTitleId = title.Id,
                    Version = appInfo.Version,
                    InstallDate = appInfo.InstallDate
                });
            }

            await db.SaveChangesAsync();
            return Results.Ok(computer);
        }).WithName("inventory/computer");

        app.MapGet("/inventory/computers", async (ItsmDbContext db) =>
        {
            var computers = await db.Computers
                .Include(c => c.Asset)
                .Include(c => c.Disks)
                .Include(c => c.NetworkInterfaces)
                .Include(c => c.ComputerGpus).ThenInclude(cg => cg.GpuModel)
                .Include(c => c.ComputerSoftware).ThenInclude(cs => cs.SoftwareTitle)
                .OrderBy(c => c.ComputerName)
                .ToListAsync();

            return computers.Select(ToWireFormat).ToList();
        });

        app.MapGet("/inventory/computers/{computerName}", async (string computerName, ItsmDbContext db) =>
        {
            var comp = await db.Computers
                .Include(c => c.Asset)
                .Include(c => c.Disks)
                .Include(c => c.NetworkInterfaces)
                .Include(c => c.ComputerGpus).ThenInclude(cg => cg.GpuModel)
                .Include(c => c.ComputerSoftware).ThenInclude(cs => cs.SoftwareTitle)
                .FirstOrDefaultAsync(c => c.ComputerName == computerName);

            return comp is null ? Results.NotFound() : Results.Ok(ToWireFormat(comp));
        });
    }

    private static object ToWireFormat(ComputerEntity c) => new
    {
        computerName = c.ComputerName,
        lastUpdatedUtc = c.LastUpdatedUtc,
        data = new
        {
            identity = new
            {
                computerName = c.ComputerName,
                modelName = c.ModelName,
                serialNumber = c.Asset?.SerialNumber,
                hardwareUuid = c.HardwareUuid,
                loggedInUser = c.LoggedInUser,
                chassisType = c.ChassisType
            },
            cpu = new
            {
                brandString = c.CpuBrand,
                coreCount = c.CpuCores,
                architecture = c.CpuArchitecture
            },
            memory = new { totalBytes = c.TotalMemoryBytes },
            disks = c.Disks.Select(d => new
            {
                name = d.Name,
                format = d.Format,
                totalBytes = d.TotalBytes,
                freeBytes = d.FreeBytes
            }),
            os = new
            {
                description = c.OsDescription,
                version = c.OsVersion,
                buildNumber = c.OsBuildNumber
            },
            network = new
            {
                hostname = c.Hostname,
                interfaces = c.NetworkInterfaces.Select(n => new
                {
                    name = n.Name,
                    macAddress = n.MacAddress,
                    ipAddresses = n.IpAddresses
                })
            },
            gpus = c.ComputerGpus.Select(g => new
            {
                name = g.GpuModel.Name,
                vendor = g.GpuModel.Vendor,
                vramBytes = g.VramBytes,
                driverVersion = g.DriverVersion
            }),
            battery = new
            {
                isPresent = c.BatteryPresent,
                chargePercent = c.BatteryCharge,
                cycleCount = c.BatteryCycles,
                healthPercent = c.BatteryHealth,
                isCharging = c.BatteryCharging,
                condition = c.BatteryCondition
            },
            installedApps = c.ComputerSoftware.Select(s => new
            {
                name = s.SoftwareTitle.Name,
                version = s.Version,
                installDate = s.InstallDate
            }),
            uptime = new
            {
                lastBootUtc = c.LastBootUtc,
                uptime = c.Uptime
            },
            firewall = new
            {
                isEnabled = c.FirewallEnabled,
                stealthMode = c.FirewallStealth
            },
            encryption = new
            {
                isEnabled = c.EncryptionEnabled,
                method = c.EncryptionMethod
            }
        }
    };
}
