namespace Itsm.Common.Models;

public record Computer(
    MachineIdentity Identity,
    CpuInfo Cpu,
    MemoryInfo Memory,
    List<DiskInfo> Disks,
    OsInfo Os,
    NetworkInfo Network);
