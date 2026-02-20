using Itsm.Common.Models;

namespace Itsm.Agent;

public interface IHardwareGatherer
{
    CpuInfo GetCpuInformation();
    MemoryInfo GetMemoryInformation();
    List<DiskInfo> GetDiskInformation();
    OsInfo GetOsInformation();
    NetworkInfo GetNetworkInformation();
    MachineIdentity GetMachineIdentity();
    List<GpuInfo> GetGpuInformation();
    BatteryInfo GetBatteryInformation();
    List<InstalledApp> GetInstalledApplications();
    UptimeInfo GetUptimeInformation();
    FirewallInfo GetFirewallInformation();
    EncryptionInfo GetEncryptionInformation();
    BiosInfo GetBiosInformation();
    MotherboardInfo GetMotherboardInformation();
    List<AntivirusInfo> GetAntivirusInformation();
    List<SystemController> GetControllers();
    VirtualizationInfo GetVirtualizationInformation();
    List<DatabaseInstanceInfo> GetDatabaseInstances();
    Task<LocationInfo?> GetLocationAsync();
}
