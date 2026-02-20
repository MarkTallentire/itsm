namespace Itsm.Api;

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
    public int? CpuThreads { get; set; }
    public int? CpuSpeedMHz { get; set; }
    public string CpuArchitecture { get; set; } = "";
    public long TotalMemoryBytes { get; set; }
    public string OsDescription { get; set; } = "";
    public string? OsVersion { get; set; }
    public string? OsBuildNumber { get; set; }
    public string? OsKernelName { get; set; }
    public string? OsKernelVersion { get; set; }
    public string? OsArchitecture { get; set; }
    public string? OsInstallDate { get; set; }
    public string? OsLicenseKey { get; set; }
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

    // BIOS/Firmware
    public string? BiosManufacturer { get; set; }
    public string? BiosVersion { get; set; }
    public string? BiosReleaseDate { get; set; }
    public string? BiosSerial { get; set; }

    // Motherboard
    public string? MotherboardManufacturer { get; set; }
    public string? MotherboardProduct { get; set; }
    public string? MotherboardSerial { get; set; }
    public string? MotherboardVersion { get; set; }

    // Location
    public double? LocationLatitude { get; set; }
    public double? LocationLongitude { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationRegion { get; set; }
    public string? LocationCountry { get; set; }
    public string? LocationTimezone { get; set; }
    public string? PublicIp { get; set; }

    // DNS
    public string? DnsDomain { get; set; }

    public AssetRecord Asset { get; set; } = null!;
    public List<DiskEntity> Disks { get; set; } = [];
    public List<NetworkInterfaceEntity> NetworkInterfaces { get; set; } = [];
    public List<ComputerGpuEntity> ComputerGpus { get; set; } = [];
    public List<ComputerSoftwareEntity> ComputerSoftware { get; set; } = [];
    public List<MemoryModuleEntity> MemoryModules { get; set; } = [];
    public List<AntivirusEntity> AntivirusProducts { get; set; } = [];
    public List<ControllerEntity> Controllers { get; set; } = [];
    public List<VmInstanceEntity> VirtualMachines { get; set; } = [];
    public List<DockerContainerEntity> DockerContainers { get; set; } = [];
    public List<DatabaseInstanceEntity> DatabaseInstances { get; set; } = [];
    public List<VpnConnectionEntity> VpnConnections { get; set; } = [];
    public List<NetworkDriveEntity> NetworkDrives { get; set; } = [];
    public List<ListeningPortEntity> ListeningPorts { get; set; } = [];
    public List<DnsServerEntity> DnsServers { get; set; } = [];
    public List<DnsSearchDomainEntity> DnsSearchDomains { get; set; } = [];
}
