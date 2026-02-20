using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Itsm.Common;
using Itsm.Common.Models;

namespace Itsm.Agent;

public class MacHardwareGatherer(ICommandRunner commandRunner) : IHardwareGatherer
{
    public CpuInfo GetCpuInformation()
    {
        var coreCount = Environment.ProcessorCount;
        var architecture = RuntimeInformation.ProcessArchitecture.ToString();
        var brandString = commandRunner.Run("sysctl", "-n machdep.cpu.brand_string");

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
        var profileOutput = commandRunner.Run("system_profiler", "SPHardwareDataType");

        var serialNumber = "Unknown";
        var modelName = "Unknown";
        var hardwareUuid = "Unknown";

        foreach (var line in profileOutput.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Serial Number"))
                serialNumber = trimmed.Split(':').Last().Trim();
            else if (trimmed.StartsWith("Model Name"))
                modelName = trimmed.Split(':').Last().Trim();
            else if (trimmed.StartsWith("Hardware UUID"))
                hardwareUuid = trimmed.Split(':', 2).Last().Trim();
        }

        var computerName = commandRunner.Run("scutil", "--get ComputerName");
        var loggedInUser = Environment.UserName;
        var chassisType = ClassifyChassisType(modelName);

        return new MachineIdentity(computerName, modelName, serialNumber, hardwareUuid, loggedInUser, chassisType);
    }

    private static ChassisType ClassifyChassisType(string modelName)
    {
        if (modelName.Contains("MacBook", StringComparison.OrdinalIgnoreCase))
            return ChassisType.Laptop;
        if (modelName.Contains("iMac", StringComparison.OrdinalIgnoreCase))
            return ChassisType.AllInOne;
        if (modelName.Contains("Mac mini", StringComparison.OrdinalIgnoreCase))
            return ChassisType.Mini;
        if (modelName.Contains("Mac Pro", StringComparison.OrdinalIgnoreCase))
            return ChassisType.Tower;
        if (modelName.Contains("Mac Studio", StringComparison.OrdinalIgnoreCase))
            return ChassisType.Desktop;
        return ChassisType.Unknown;
    }
}
