namespace Itsm.Common.Models;

public record Computer(
    MachineIdentity Identity,
    CpuInfo Cpu,
    MemoryInfo Memory,
    List<DiskInfo> Disks,
    OsInfo Os,
    NetworkInfo Network,
    List<GpuInfo> Gpus,
    BatteryInfo Battery,
    List<InstalledApp> InstalledApps,
    UptimeInfo Uptime,
    FirewallInfo Firewall,
    EncryptionInfo Encryption,
    BiosInfo Bios,
    MotherboardInfo Motherboard,
    List<AntivirusInfo> Antivirus,
    List<SystemController> Controllers,
    VirtualizationInfo Virtualization,
    List<DatabaseInstanceInfo> Databases,
    LocationInfo? Location);
