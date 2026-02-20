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
            if (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable) continue;
            if (drive.TotalSize == 0) continue;
            disks.Add(new DiskInfo(drive.Name, drive.DriveFormat, drive.TotalSize, drive.AvailableFreeSpace));
        }
        return disks;
    }

    public OsInfo GetOsInformation()
    {
        try
        {
            var output = commandRunner.Run("wmic", "os get Caption,Version,BuildNumber /format:list");
            var caption = ParseWmicValue(output, "Caption");
            var version = ParseWmicValue(output, "Version");
            var buildNumber = ParseWmicValue(output, "BuildNumber");
            return new OsInfo(
                caption != "Unknown" ? caption : RuntimeInformation.OSDescription,
                version != "Unknown" ? version : null,
                buildNumber != "Unknown" ? buildNumber : null);
        }
        catch
        {
            return new OsInfo(RuntimeInformation.OSDescription, null, null);
        }
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

    public List<GpuInfo> GetGpuInformation()
    {
        try
        {
            var output = commandRunner.Run("wmic", "path win32_VideoController get Name,AdapterRAM,DriverVersion /format:list");
            var gpus = new List<GpuInfo>();

            // wmic list format separates records by blank lines; each record has Name=, AdapterRAM=, DriverVersion=
            var blocks = output.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var block in blocks)
            {
                var name = ParseWmicValue(block, "Name");
                if (name == "Unknown") continue;

                var ramStr = ParseWmicValue(block, "AdapterRAM");
                long? vramBytes = long.TryParse(ramStr, out var ram) ? ram : null;

                var driverVersion = ParseWmicValue(block, "DriverVersion");

                // Derive vendor from GPU name
                var vendor = "Unknown";
                if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                    vendor = "NVIDIA";
                else if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                    vendor = "AMD";
                else if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                    vendor = "Intel";

                gpus.Add(new GpuInfo(name, vendor, vramBytes, driverVersion != "Unknown" ? driverVersion : null));
            }

            return gpus;
        }
        catch
        {
            return [];
        }
    }

    public BatteryInfo GetBatteryInformation()
    {
        try
        {
            var output = commandRunner.Run("wmic", "path Win32_Battery get BatteryStatus,EstimatedChargeRemaining /format:list");
            var chargeStr = ParseWmicValue(output, "EstimatedChargeRemaining");
            var statusStr = ParseWmicValue(output, "BatteryStatus");

            if (chargeStr == "Unknown" && statusStr == "Unknown")
                return new BatteryInfo(false, null, null, null, null, null);

            double? chargePercent = double.TryParse(chargeStr, out var charge) ? charge : null;
            // BatteryStatus: 1=Discharging, 2=AC connected (charging), 3-5 various charged states
            bool? isCharging = int.TryParse(statusStr, out var status) ? status == 2 : null;

            // Try to get cycle count and health from WMI via PowerShell
            int? cycleCount = null;
            double? healthPercent = null;
            string? condition = null;

            try
            {
                var psOutput = commandRunner.Run("powershell", "-Command \"Get-CimInstance -ClassName Win32_Battery | Select-Object -Property EstimatedRunTime,Status | Format-List\"");
                var batteryStatus = ParsePowerShellValue(psOutput, "Status");
                if (batteryStatus != "Unknown")
                    condition = batteryStatus;
            }
            catch { /* optional data, ignore failures */ }

            try
            {
                var designCap = commandRunner.Run("powershell", "-Command \"(Get-CimInstance -Namespace root/WMI -ClassName BatteryStaticData).DesignedCapacity\"");
                var fullCap = commandRunner.Run("powershell", "-Command \"(Get-CimInstance -Namespace root/WMI -ClassName BatteryFullChargedCapacity).FullChargedCapacity\"");
                if (long.TryParse(designCap.Trim(), out var designed) && long.TryParse(fullCap.Trim(), out var full) && designed > 0)
                    healthPercent = Math.Round((double)full / designed * 100, 1);

                var cycleOutput = commandRunner.Run("powershell", "-Command \"(Get-CimInstance -Namespace root/WMI -ClassName BatteryCycleCount).CycleCount\"");
                if (int.TryParse(cycleOutput.Trim(), out var cycles))
                    cycleCount = cycles;
            }
            catch { /* WMI battery namespace may not be available on all systems */ }

            return new BatteryInfo(true, chargePercent, cycleCount, healthPercent, isCharging, condition);
        }
        catch
        {
            return new BatteryInfo(false, null, null, null, null, null);
        }
    }

    public List<InstalledApp> GetInstalledApplications()
    {
        try
        {
            var script = @"
$paths = @(
    'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
)
foreach ($path in $paths) {
    Get-ItemProperty $path -ErrorAction SilentlyContinue |
        Where-Object { $_.DisplayName } |
        Select-Object DisplayName, DisplayVersion, InstallDate |
        ForEach-Object { ""$($_.DisplayName)|$($_.DisplayVersion)|$($_.InstallDate)"" }
}";
            var output = commandRunner.Run("powershell", $"-Command \"{script.Replace("\"", "\\\"")}\"");

            var apps = new Dictionary<string, InstalledApp>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Trim().Split('|');
                if (parts.Length < 1 || string.IsNullOrWhiteSpace(parts[0])) continue;

                var name = parts[0].Trim();
                var version = parts.Length > 1 ? parts[1].Trim() : "";
                var installDate = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : null;

                // Deduplicate by name (32-bit and 64-bit registry can have same app)
                if (!apps.ContainsKey(name))
                    apps[name] = new InstalledApp(name, version, installDate);
            }

            return apps.Values.OrderBy(a => a.Name).ToList();
        }
        catch
        {
            return [];
        }
    }

    public UptimeInfo GetUptimeInformation()
    {
        try
        {
            var output = commandRunner.Run("wmic", "os get LastBootUpTime /format:list");
            var raw = ParseWmicValue(output, "LastBootUpTime");

            // WMI datetime format: 20250215120000.000000+000
            if (raw.Length >= 14)
            {
                var year = int.Parse(raw[..4]);
                var month = int.Parse(raw[4..6]);
                var day = int.Parse(raw[6..8]);
                var hour = int.Parse(raw[8..10]);
                var minute = int.Parse(raw[10..12]);
                var second = int.Parse(raw[12..14]);

                // Parse timezone offset (e.g., +000 means UTC offset in minutes)
                var lastBoot = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local).ToUniversalTime();
                var uptime = DateTime.UtcNow - lastBoot;
                return new UptimeInfo(lastBoot, uptime);
            }

            return new UptimeInfo(DateTime.UtcNow, TimeSpan.Zero);
        }
        catch
        {
            return new UptimeInfo(DateTime.UtcNow, TimeSpan.Zero);
        }
    }

    public FirewallInfo GetFirewallInformation()
    {
        try
        {
            var output = commandRunner.Run("netsh", "advfirewall show allprofiles state");
            // Output contains lines like "State                                 ON" for each profile
            var isEnabled = output.Contains("ON", StringComparison.OrdinalIgnoreCase);
            return new FirewallInfo(isEnabled, null);
        }
        catch
        {
            return new FirewallInfo(false, null);
        }
    }

    public EncryptionInfo GetEncryptionInformation()
    {
        try
        {
            var output = commandRunner.Run("powershell", "-Command \"manage-bde -status C:\"");

            // Look for "Protection Status: Protection On" or "Conversion Status: Fully Encrypted"
            var isEnabled = output.Contains("Protection On", StringComparison.OrdinalIgnoreCase)
                         || output.Contains("Fully Encrypted", StringComparison.OrdinalIgnoreCase);

            string? method = null;
            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Encryption Method:", StringComparison.OrdinalIgnoreCase))
                {
                    method = trimmed["Encryption Method:".Length..].Trim();
                    if (string.IsNullOrWhiteSpace(method) || method.Equals("None", StringComparison.OrdinalIgnoreCase))
                        method = null;
                    break;
                }
            }

            return new EncryptionInfo(isEnabled, isEnabled ? method ?? "BitLocker" : null);
        }
        catch
        {
            // manage-bde may require admin privileges
            return new EncryptionInfo(false, null);
        }
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

    internal static string ParsePowerShellValue(string output, string key)
    {
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith($"{key}", StringComparison.OrdinalIgnoreCase))
            {
                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex >= 0)
                    return trimmed[(colonIndex + 1)..].Trim();
            }
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
