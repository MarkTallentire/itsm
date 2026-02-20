using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Itsm.Common;
using Itsm.Common.Models;

namespace Itsm.Agent;

public class LinuxHardwareGatherer(ICommandRunner commandRunner) : IHardwareGatherer
{
    public CpuInfo GetCpuInformation()
    {
        var coreCount = Environment.ProcessorCount;
        var architecture = RuntimeInformation.ProcessArchitecture.ToString();
        var cpuInfo = commandRunner.Run("cat", "/proc/cpuinfo");
        var brandString = "Unknown";

        foreach (var line in cpuInfo.Split('\n'))
        {
            if (line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))
            {
                brandString = line.Split(':', 2).Last().Trim();
                break;
            }
        }

        return new CpuInfo(brandString, coreCount, architecture);
    }

    public MemoryInfo GetMemoryInformation()
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        return new MemoryInfo(gcMemoryInfo.TotalAvailableMemoryBytes);
    }

    public List<DiskInfo> GetDiskInformation()
    {
        var disks = new List<DiskInfo>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;
            disks.Add(new DiskInfo(drive.Name, drive.DriveFormat, drive.TotalSize, drive.AvailableFreeSpace));
        }
        return disks;
    }

    public OsInfo GetOsInformation()
    {
        return new OsInfo(RuntimeInformation.OSDescription);
    }

    public NetworkInfo GetNetworkInformation()
    {
        var interfaces = new List<NetworkInterfaceInfo>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;

            var mac = nic.GetPhysicalAddress().ToString();
            var addresses = nic.GetIPProperties().UnicastAddresses
                .Select(a => a.Address.ToString())
                .ToList();

            interfaces.Add(new NetworkInterfaceInfo(nic.Name, mac, addresses));
        }

        return new NetworkInfo(Environment.MachineName, interfaces);
    }

    public MachineIdentity GetMachineIdentity()
    {
        var computerName = commandRunner.Run("hostname", "");
        var loggedInUser = Environment.UserName;

        var modelName = commandRunner.Run("cat", "/sys/class/dmi/id/product_name");
        var serialNumber = commandRunner.Run("cat", "/sys/class/dmi/id/product_serial");
        var hardwareUuid = commandRunner.Run("cat", "/sys/class/dmi/id/product_uuid");
        var chassisCode = commandRunner.Run("cat", "/sys/class/dmi/id/chassis_type");
        var chassisType = ParseChassisType(chassisCode);

        return new MachineIdentity(computerName, modelName, serialNumber, hardwareUuid, loggedInUser, chassisType);
    }

    internal static ChassisType ParseChassisType(string chassisCode)
    {
        if (!int.TryParse(chassisCode.Trim(), out var code))
            return ChassisType.Unknown;

        // Same SMBIOS chassis type codes as WMI
        return code switch
        {
            3 or 4 or 5 or 6 => ChassisType.Desktop,
            7 => ChassisType.Tower,
            8 or 9 or 10 or 14 => ChassisType.Laptop,
            13 => ChassisType.AllInOne,
            30 or 31 or 32 => ChassisType.Tablet,
            34 or 35 or 36 => ChassisType.Mini,
            _ => ChassisType.Unknown
        };
    }
}
