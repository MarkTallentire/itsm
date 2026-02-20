using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Itsm.Common;
using Itsm.Common.Models;

namespace Itsm.Agent;

public class WindowsHardwareGatherer(ICommandRunner commandRunner) : IHardwareGatherer
{
    public CpuInfo GetCpuInformation()
    {
        var coreCount = Environment.ProcessorCount;
        var architecture = RuntimeInformation.ProcessArchitecture.ToString();
        var output = commandRunner.Run("wmic", "cpu get Name /format:value");
        var brandString = ParseWmicValue(output, "Name");

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

        var modelOutput = commandRunner.Run("wmic", "computersystem get Model /format:value");
        var modelName = ParseWmicValue(modelOutput, "Model");

        var serialOutput = commandRunner.Run("wmic", "bios get SerialNumber /format:value");
        var serialNumber = ParseWmicValue(serialOutput, "SerialNumber");

        var uuidOutput = commandRunner.Run("wmic", "csproduct get UUID /format:value");
        var hardwareUuid = ParseWmicValue(uuidOutput, "UUID");

        var chassisOutput = commandRunner.Run("wmic", "systemenclosure get ChassisTypes /format:value");
        var chassisType = ParseChassisType(chassisOutput);

        return new MachineIdentity(computerName, modelName, serialNumber, hardwareUuid, loggedInUser, chassisType);
    }

    internal static string ParseWmicValue(string output, string key)
    {
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                return trimmed[(key.Length + 1)..].Trim();
        }
        return "Unknown";
    }

    internal static ChassisType ParseChassisType(string chassisOutput)
    {
        var raw = ParseWmicValue(chassisOutput, "ChassisTypes");
        // WMI returns e.g. "{9}" or "{3}"
        var cleaned = raw.Trim('{', '}', ' ');
        if (!int.TryParse(cleaned, out var code))
            return ChassisType.Unknown;

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
