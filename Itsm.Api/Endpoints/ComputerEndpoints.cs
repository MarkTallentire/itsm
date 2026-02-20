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

            var existing = await db.Computers
                .Include(c => c.Disks)
                .Include(c => c.NetworkInterfaces)
                .Include(c => c.ComputerGpus)
                .Include(c => c.ComputerSoftware)
                .Include(c => c.MemoryModules)
                .Include(c => c.AntivirusProducts)
                .Include(c => c.Controllers)
                .Include(c => c.VirtualMachines)
                .Include(c => c.DockerContainers)
                .Include(c => c.DatabaseInstances)
                .Include(c => c.VpnConnections)
                .Include(c => c.NetworkDrives)
                .Include(c => c.ListeningPorts)
                .Include(c => c.DnsServers)
                .Include(c => c.DnsSearchDomains)
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
                db.MemoryModules.RemoveRange(comp.MemoryModules);
                db.AntivirusProducts.RemoveRange(comp.AntivirusProducts);
                db.Controllers.RemoveRange(comp.Controllers);
                db.VirtualMachines.RemoveRange(comp.VirtualMachines);
                db.DockerContainers.RemoveRange(comp.DockerContainers);
                db.DatabaseInstances.RemoveRange(comp.DatabaseInstances);
                db.VpnConnections.RemoveRange(comp.VpnConnections);
                db.NetworkDrives.RemoveRange(comp.NetworkDrives);
                db.ListeningPorts.RemoveRange(comp.ListeningPorts);
                db.DnsServers.RemoveRange(comp.DnsServers);
                db.DnsSearchDomains.RemoveRange(comp.DnsSearchDomains);
            }

            // ── Scalar fields ──
            comp.ComputerName = name;
            comp.ModelName = computer.Identity.ModelName;
            comp.HardwareUuid = computer.Identity.HardwareUuid;
            comp.LoggedInUser = computer.Identity.LoggedInUser;
            comp.ChassisType = computer.Identity.ChassisType.ToString();
            comp.CpuBrand = computer.Cpu.BrandString;
            comp.CpuCores = computer.Cpu.CoreCount;
            comp.CpuThreads = computer.Cpu.ThreadCount;
            comp.CpuSpeedMHz = computer.Cpu.SpeedMHz;
            comp.CpuArchitecture = computer.Cpu.Architecture;
            comp.TotalMemoryBytes = computer.Memory.TotalBytes;
            comp.OsDescription = computer.Os.Description;
            comp.OsVersion = computer.Os.Version;
            comp.OsBuildNumber = computer.Os.BuildNumber;
            comp.OsKernelName = computer.Os.KernelName;
            comp.OsKernelVersion = computer.Os.KernelVersion;
            comp.OsArchitecture = computer.Os.Architecture;
            comp.OsInstallDate = computer.Os.InstallDate;
            comp.OsLicenseKey = computer.Os.LicenseKey;
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

            // BIOS
            comp.BiosManufacturer = computer.Bios.Manufacturer;
            comp.BiosVersion = computer.Bios.Version;
            comp.BiosReleaseDate = computer.Bios.ReleaseDate;
            comp.BiosSerial = computer.Bios.Serial;

            // Motherboard
            comp.MotherboardManufacturer = computer.Motherboard.Manufacturer;
            comp.MotherboardProduct = computer.Motherboard.Product;
            comp.MotherboardSerial = computer.Motherboard.Serial;
            comp.MotherboardVersion = computer.Motherboard.Version;

            // Location
            if (computer.Location is not null)
            {
                comp.LocationLatitude = computer.Location.Latitude;
                comp.LocationLongitude = computer.Location.Longitude;
                comp.LocationCity = computer.Location.City;
                comp.LocationRegion = computer.Location.Region;
                comp.LocationCountry = computer.Location.Country;
                comp.LocationTimezone = computer.Location.Timezone;
                comp.PublicIp = computer.Location.PublicIp;
            }

            // DNS
            comp.DnsDomain = computer.Network.Dns.Domain;

            // ── Child rows ──

            // Disks
            foreach (var d in computer.Disks)
                db.Disks.Add(new DiskEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, Name = d.Name, Format = d.Format, TotalBytes = d.TotalBytes, FreeBytes = d.FreeBytes });

            // Network interfaces
            foreach (var iface in computer.Network.Interfaces)
                db.NetworkInterfaces.Add(new NetworkInterfaceEntity
                {
                    Id = Guid.NewGuid(), ComputerId = comp.Id, Name = iface.Name,
                    MacAddress = iface.MacAddress, IpAddresses = iface.IpAddresses.ToArray(),
                    SpeedMbps = iface.SpeedMbps, InterfaceType = iface.InterfaceType,
                    IsDhcp = iface.IsDhcp, Gateway = iface.Gateway, SubnetMask = iface.SubnetMask,
                    WifiSsid = iface.WifiSsid, WifiFrequencyGHz = iface.WifiFrequencyGHz, WifiSignalDbm = iface.WifiSignalDbm
                });

            // GPUs
            foreach (var gpu in computer.Gpus)
            {
                var model = await db.GpuModels.FirstOrDefaultAsync(g => g.Name == gpu.Name && g.Vendor == gpu.Vendor)
                    ?? db.GpuModels.Local.FirstOrDefault(g => g.Name == gpu.Name && g.Vendor == gpu.Vendor);
                if (model is null)
                {
                    model = new GpuModelEntity { Id = Guid.NewGuid(), Name = gpu.Name, Vendor = gpu.Vendor };
                    db.GpuModels.Add(model);
                }
                db.ComputerGpus.Add(new ComputerGpuEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, GpuModelId = model.Id, VramBytes = gpu.VramBytes, DriverVersion = gpu.DriverVersion });
            }

            // Software
            foreach (var appInfo in computer.InstalledApps)
            {
                var title = await db.SoftwareTitles.FirstOrDefaultAsync(s => s.Name == appInfo.Name)
                    ?? db.SoftwareTitles.Local.FirstOrDefault(s => s.Name == appInfo.Name);
                if (title is null)
                {
                    title = new SoftwareTitleEntity { Id = Guid.NewGuid(), Name = appInfo.Name, Publisher = appInfo.Publisher };
                    db.SoftwareTitles.Add(title);
                }
                else if (appInfo.Publisher != null)
                {
                    title.Publisher = appInfo.Publisher;
                }
                db.ComputerSoftware.Add(new ComputerSoftwareEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, SoftwareTitleId = title.Id, Version = appInfo.Version, InstallDate = appInfo.InstallDate });
            }

            // Memory modules
            foreach (var m in computer.Memory.Modules)
                db.MemoryModules.Add(new MemoryModuleEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, SlotLabel = m.SlotLabel, CapacityBytes = m.CapacityBytes, SpeedMHz = m.SpeedMHz, Type = m.Type, Manufacturer = m.Manufacturer, SerialNumber = m.SerialNumber });

            // Antivirus
            foreach (var av in computer.Antivirus)
                db.AntivirusProducts.Add(new AntivirusEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, Name = av.Name, Version = av.Version, IsEnabled = av.IsEnabled, IsUpToDate = av.IsUpToDate, ExpirationDate = av.ExpirationDate });

            // Controllers
            foreach (var ctrl in computer.Controllers)
                db.Controllers.Add(new ControllerEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, Name = ctrl.Name, Manufacturer = ctrl.Manufacturer, Type = ctrl.Type, PciId = ctrl.PciId });

            // Virtual machines
            foreach (var vm in computer.Virtualization.VirtualMachines)
                db.VirtualMachines.Add(new VmInstanceEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, Name = vm.Name, State = vm.State, Type = vm.Type, MemoryMB = vm.MemoryMB, CpuCount = vm.CpuCount });

            // Docker containers
            foreach (var dc in computer.Virtualization.DockerContainers)
                db.DockerContainers.Add(new DockerContainerEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, ContainerId = dc.Id, Name = dc.Name, Image = dc.Image, State = dc.State, Status = dc.Status });

            // Database instances
            foreach (var dbi in computer.Databases)
                db.DatabaseInstances.Add(new DatabaseInstanceEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, Name = dbi.Name, Version = dbi.Version, Port = dbi.Port, IsRunning = dbi.IsRunning });

            // VPN connections
            foreach (var vpn in computer.Network.VpnConnections)
                db.VpnConnections.Add(new VpnConnectionEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, Name = vpn.Name, Type = vpn.Type, ServerAddress = vpn.ServerAddress, IsConnected = vpn.IsConnected });

            // Network drives
            foreach (var nd in computer.Network.NetworkDrives)
                db.NetworkDrives.Add(new NetworkDriveEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, LocalPath = nd.LocalPath, RemotePath = nd.RemotePath, FileSystem = nd.FileSystem });

            // Listening ports
            foreach (var lp in computer.Network.ListeningPorts)
                db.ListeningPorts.Add(new ListeningPortEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, Port = lp.Port, Protocol = lp.Protocol, ProcessName = lp.ProcessName, Pid = lp.Pid });

            // DNS servers
            foreach (var dns in computer.Network.Dns.Servers)
                db.DnsServers.Add(new DnsServerEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, Address = dns });

            // DNS search domains
            foreach (var sd in computer.Network.Dns.SearchDomains)
                db.DnsSearchDomains.Add(new DnsSearchDomainEntity { Id = Guid.NewGuid(), ComputerId = comp.Id, Domain = sd });

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

            return computers.Select(ToListFormat).ToList();
        });

        app.MapGet("/inventory/computers/{computerName}", async (string computerName, ItsmDbContext db) =>
        {
            var comp = await db.Computers
                .Include(c => c.Asset)
                .Include(c => c.Disks)
                .Include(c => c.NetworkInterfaces)
                .Include(c => c.ComputerGpus).ThenInclude(cg => cg.GpuModel)
                .Include(c => c.ComputerSoftware).ThenInclude(cs => cs.SoftwareTitle)
                .Include(c => c.MemoryModules)
                .Include(c => c.AntivirusProducts)
                .Include(c => c.Controllers)
                .Include(c => c.VirtualMachines)
                .Include(c => c.DockerContainers)
                .Include(c => c.DatabaseInstances)
                .Include(c => c.VpnConnections)
                .Include(c => c.NetworkDrives)
                .Include(c => c.ListeningPorts)
                .Include(c => c.DnsServers)
                .Include(c => c.DnsSearchDomains)
                .FirstOrDefaultAsync(c => c.ComputerName == computerName);

            return comp is null ? Results.NotFound() : Results.Ok(ToDetailFormat(comp));
        });
    }

    // Lightweight format for the list view — skip heavy child data
    private static object ToListFormat(ComputerEntity c) => new
    {
        computerName = c.ComputerName,
        lastUpdatedUtc = c.LastUpdatedUtc,
        data = new
        {
            identity = new { computerName = c.ComputerName, modelName = c.ModelName, serialNumber = c.Asset?.SerialNumber, hardwareUuid = c.HardwareUuid, loggedInUser = c.LoggedInUser, chassisType = c.ChassisType },
            cpu = new { brandString = c.CpuBrand, coreCount = c.CpuCores, threadCount = c.CpuThreads, architecture = c.CpuArchitecture, speedMHz = c.CpuSpeedMHz },
            memory = new { totalBytes = c.TotalMemoryBytes },
            disks = c.Disks.Select(d => new { name = d.Name, format = d.Format, totalBytes = d.TotalBytes, freeBytes = d.FreeBytes }),
            os = new { description = c.OsDescription, version = c.OsVersion, buildNumber = c.OsBuildNumber },
            network = new { hostname = c.Hostname, interfaces = c.NetworkInterfaces.Select(n => new { name = n.Name, macAddress = n.MacAddress, ipAddresses = n.IpAddresses }) },
            gpus = c.ComputerGpus.Select(g => new { name = g.GpuModel.Name, vendor = g.GpuModel.Vendor, vramBytes = g.VramBytes, driverVersion = g.DriverVersion }),
            battery = new { isPresent = c.BatteryPresent, chargePercent = c.BatteryCharge, cycleCount = c.BatteryCycles, healthPercent = c.BatteryHealth, isCharging = c.BatteryCharging, condition = c.BatteryCondition },
            uptime = new { lastBootUtc = c.LastBootUtc, uptime = c.Uptime },
            firewall = new { isEnabled = c.FirewallEnabled, stealthMode = c.FirewallStealth },
            encryption = new { isEnabled = c.EncryptionEnabled, method = c.EncryptionMethod }
        }
    };

    // Full format for the detail view — includes all data
    private static object ToDetailFormat(ComputerEntity c) => new
    {
        computerName = c.ComputerName,
        lastUpdatedUtc = c.LastUpdatedUtc,
        data = new
        {
            identity = new { computerName = c.ComputerName, modelName = c.ModelName, serialNumber = c.Asset?.SerialNumber, hardwareUuid = c.HardwareUuid, loggedInUser = c.LoggedInUser, chassisType = c.ChassisType },
            cpu = new { brandString = c.CpuBrand, coreCount = c.CpuCores, threadCount = c.CpuThreads, architecture = c.CpuArchitecture, speedMHz = c.CpuSpeedMHz },
            memory = new
            {
                totalBytes = c.TotalMemoryBytes,
                modules = c.MemoryModules.Select(m => new { slotLabel = m.SlotLabel, capacityBytes = m.CapacityBytes, speedMHz = m.SpeedMHz, type = m.Type, manufacturer = m.Manufacturer, serialNumber = m.SerialNumber })
            },
            disks = c.Disks.Select(d => new { name = d.Name, format = d.Format, totalBytes = d.TotalBytes, freeBytes = d.FreeBytes }),
            os = new { description = c.OsDescription, version = c.OsVersion, buildNumber = c.OsBuildNumber, kernelName = c.OsKernelName, kernelVersion = c.OsKernelVersion, architecture = c.OsArchitecture, installDate = c.OsInstallDate, licenseKey = c.OsLicenseKey },
            network = new
            {
                hostname = c.Hostname,
                interfaces = c.NetworkInterfaces.Select(n => new
                {
                    name = n.Name, macAddress = n.MacAddress, ipAddresses = n.IpAddresses,
                    speedMbps = n.SpeedMbps, interfaceType = n.InterfaceType, isDhcp = n.IsDhcp,
                    gateway = n.Gateway, subnetMask = n.SubnetMask,
                    wifiSsid = n.WifiSsid, wifiFrequencyGHz = n.WifiFrequencyGHz, wifiSignalDbm = n.WifiSignalDbm
                }),
                vpnConnections = c.VpnConnections.Select(v => new { name = v.Name, type = v.Type, serverAddress = v.ServerAddress, isConnected = v.IsConnected }),
                dns = new { servers = c.DnsServers.Select(d => d.Address), domain = c.DnsDomain, searchDomains = c.DnsSearchDomains.Select(d => d.Domain) },
                networkDrives = c.NetworkDrives.Select(n => new { localPath = n.LocalPath, remotePath = n.RemotePath, fileSystem = n.FileSystem }),
                listeningPorts = c.ListeningPorts.Select(l => new { port = l.Port, protocol = l.Protocol, processName = l.ProcessName, pid = l.Pid })
            },
            gpus = c.ComputerGpus.Select(g => new { name = g.GpuModel.Name, vendor = g.GpuModel.Vendor, vramBytes = g.VramBytes, driverVersion = g.DriverVersion }),
            battery = new { isPresent = c.BatteryPresent, chargePercent = c.BatteryCharge, cycleCount = c.BatteryCycles, healthPercent = c.BatteryHealth, isCharging = c.BatteryCharging, condition = c.BatteryCondition },
            installedApps = c.ComputerSoftware.Select(s => new { name = s.SoftwareTitle.Name, version = s.Version, installDate = s.InstallDate, publisher = s.SoftwareTitle.Publisher }),
            uptime = new { lastBootUtc = c.LastBootUtc, uptime = c.Uptime },
            firewall = new { isEnabled = c.FirewallEnabled, stealthMode = c.FirewallStealth },
            encryption = new { isEnabled = c.EncryptionEnabled, method = c.EncryptionMethod },
            bios = new { manufacturer = c.BiosManufacturer, version = c.BiosVersion, releaseDate = c.BiosReleaseDate, serial = c.BiosSerial },
            motherboard = new { manufacturer = c.MotherboardManufacturer, product = c.MotherboardProduct, serial = c.MotherboardSerial, version = c.MotherboardVersion },
            antivirus = c.AntivirusProducts.Select(a => new { name = a.Name, version = a.Version, isEnabled = a.IsEnabled, isUpToDate = a.IsUpToDate, expirationDate = a.ExpirationDate }),
            controllers = c.Controllers.Select(ct => new { name = ct.Name, manufacturer = ct.Manufacturer, type = ct.Type, pciId = ct.PciId }),
            virtualization = new
            {
                virtualMachines = c.VirtualMachines.Select(v => new { name = v.Name, state = v.State, type = v.Type, memoryMB = v.MemoryMB, cpuCount = v.CpuCount }),
                dockerContainers = c.DockerContainers.Select(d => new { id = d.ContainerId, name = d.Name, image = d.Image, state = d.State, status = d.Status })
            },
            databases = c.DatabaseInstances.Select(d => new { name = d.Name, version = d.Version, port = d.Port, isRunning = d.IsRunning }),
            location = c.LocationLatitude.HasValue ? new { latitude = c.LocationLatitude, longitude = c.LocationLongitude, city = c.LocationCity, region = c.LocationRegion, country = c.LocationCountry, timezone = c.LocationTimezone, publicIp = c.PublicIp } : null
        }
    };
}
