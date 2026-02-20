using System.Globalization;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
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
            if (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable) continue;
            if (drive.TotalSize == 0) continue;

            // Skip macOS system/virtual mount points — only keep "/" and "/System/Volumes/Data"
            var mount = drive.Name.TrimEnd('/');
            if (mount != "" && mount != "/System/Volumes/Data") continue;

            disks.Add(new DiskInfo(drive.Name, drive.DriveFormat, drive.TotalSize, drive.AvailableFreeSpace));
        }

        // "/" and "/System/Volumes/Data" are the same APFS volume — deduplicate by keeping "/"
        if (disks.Count > 1 && disks.Any(d => d.Name.TrimEnd('/') == ""))
        {
            disks.RemoveAll(d => d.Name.TrimEnd('/') == "/System/Volumes/Data");
        }

        return disks;
    }

    public OsInfo GetOsInformation()
    {
        try
        {
            var productName = commandRunner.Run("sw_vers", "-productName").Trim();
            var productVersion = commandRunner.Run("sw_vers", "-productVersion").Trim();
            var buildVersion = commandRunner.Run("sw_vers", "-buildVersion").Trim();
            var description = $"{productName} {productVersion} ({buildVersion})";
            return new OsInfo(description, productVersion, buildVersion);
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

    public List<GpuInfo> GetGpuInformation()
    {
        try
        {
            var output = commandRunner.Run("system_profiler", "SPDisplaysDataType");
            var gpus = new List<GpuInfo>();
            string? currentName = null;
            string? currentVendor = null;
            long? currentVram = null;

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("Chipset Model:"))
                {
                    // Save previous GPU if we have one
                    if (currentName != null)
                        gpus.Add(new GpuInfo(currentName, currentVendor ?? "Unknown", currentVram, null));

                    currentName = trimmed.Split(':', 2).Last().Trim();
                    currentVendor = null;
                    currentVram = null;
                }
                else if (trimmed.StartsWith("Vendor:"))
                {
                    currentVendor = trimmed.Split(':', 2).Last().Trim();
                }
                else if (trimmed.StartsWith("VRAM") || trimmed.StartsWith("Total Number of Cores:"))
                {
                    // VRAM line looks like: "VRAM (Total): 8 GB" or "VRAM (Dynamic, Max): 1536 MB"
                    var value = trimmed.Split(':', 2).Last().Trim();
                    currentVram = ParseMemoryToBytes(value);
                }
            }

            // Don't forget the last GPU
            if (currentName != null)
                gpus.Add(new GpuInfo(currentName, currentVendor ?? "Unknown", currentVram, null));

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
            var powerOutput = commandRunner.Run("system_profiler", "SPPowerDataType");

            // Check if battery information exists at all
            var hasBattery = powerOutput.Contains("Cycle Count", StringComparison.OrdinalIgnoreCase)
                          || powerOutput.Contains("Battery Information", StringComparison.OrdinalIgnoreCase);

            if (!hasBattery)
                return new BatteryInfo(false, null, null, null, null, null);

            int? cycleCount = null;
            string? condition = null;
            double? maxCapacity = null;

            foreach (var line in powerOutput.Split('\n'))
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("Cycle Count:"))
                {
                    if (int.TryParse(trimmed.Split(':').Last().Trim(), out var cc))
                        cycleCount = cc;
                }
                else if (trimmed.StartsWith("Condition:"))
                {
                    condition = trimmed.Split(':').Last().Trim();
                }
                else if (trimmed.StartsWith("Maximum Capacity:"))
                {
                    var pctStr = trimmed.Split(':').Last().Trim().TrimEnd('%');
                    if (double.TryParse(pctStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
                        maxCapacity = pct;
                }
            }

            // Use pmset to get current charge percentage and charging status
            double? chargePercent = null;
            bool? isCharging = null;
            try
            {
                var pmsetOutput = commandRunner.Run("pmset", "-g batt");
                // Output looks like: " -InternalBattery-0 (id=...)	85%; charging; ..."
                foreach (var line in pmsetOutput.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (!trimmed.Contains("InternalBattery")) continue;

                    // Extract percentage: find pattern like "85%"
                    var parts = trimmed.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var segments = part.Split(';', StringSplitOptions.TrimEntries);
                        foreach (var seg in segments)
                        {
                            if (seg.EndsWith('%'))
                            {
                                if (double.TryParse(seg.TrimEnd('%'), NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
                                    chargePercent = pct;
                            }
                            else if (seg.Equals("charging", StringComparison.OrdinalIgnoreCase))
                            {
                                isCharging = true;
                            }
                            else if (seg.Equals("discharging", StringComparison.OrdinalIgnoreCase)
                                  || seg.Equals("not charging", StringComparison.OrdinalIgnoreCase))
                            {
                                isCharging = false;
                            }
                            else if (seg.Equals("charged", StringComparison.OrdinalIgnoreCase)
                                  || seg.Equals("finishing charge", StringComparison.OrdinalIgnoreCase)
                                  || seg.Contains("AC attached", StringComparison.OrdinalIgnoreCase))
                            {
                                isCharging = false;
                            }
                        }
                    }
                }
            }
            catch
            {
                // pmset not available or failed, leave as null
            }

            return new BatteryInfo(true, chargePercent, cycleCount, maxCapacity, isCharging, condition);
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
            var json = commandRunner.Run("system_profiler", "SPApplicationsDataType -json");
            using var doc = JsonDocument.Parse(json);
            var apps = new List<InstalledApp>();

            if (doc.RootElement.TryGetProperty("SPApplicationsDataType", out var appArray))
            {
                foreach (var app in appArray.EnumerateArray())
                {
                    var name = app.TryGetProperty("_name", out var n) ? n.GetString() ?? "Unknown" : "Unknown";
                    var version = app.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";
                    string? installDate = null;
                    if (app.TryGetProperty("lastModified", out var d))
                        installDate = d.GetString();
                    else if (app.TryGetProperty("install_date", out var d2))
                        installDate = d2.GetString();

                    apps.Add(new InstalledApp(name, version, installDate));
                }
            }

            return apps;
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
            // sysctl kern.boottime returns something like:
            // kern.boottime: { sec = 1700000000, usec = 0 } Thu Nov 14 ...
            var output = commandRunner.Run("sysctl", "kern.boottime");
            var secIndex = output.IndexOf("sec = ", StringComparison.Ordinal);
            if (secIndex < 0)
                return new UptimeInfo(DateTime.UtcNow, TimeSpan.Zero);

            secIndex += "sec = ".Length;
            var commaIndex = output.IndexOf(',', secIndex);
            if (commaIndex < 0)
                return new UptimeInfo(DateTime.UtcNow, TimeSpan.Zero);

            var secStr = output[secIndex..commaIndex].Trim();
            if (!long.TryParse(secStr, out var epochSeconds))
                return new UptimeInfo(DateTime.UtcNow, TimeSpan.Zero);

            var bootTime = DateTimeOffset.FromUnixTimeSeconds(epochSeconds).UtcDateTime;
            var uptime = DateTime.UtcNow - bootTime;

            return new UptimeInfo(bootTime, uptime);
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
            var globalState = commandRunner.Run("/usr/libexec/ApplicationFirewall/socketfilterfw", "--getglobalstate");
            var isEnabled = globalState.Contains("enabled", StringComparison.OrdinalIgnoreCase)
                         && !globalState.Contains("disabled", StringComparison.OrdinalIgnoreCase);

            bool? stealthMode = null;
            try
            {
                var stealthOutput = commandRunner.Run("/usr/libexec/ApplicationFirewall/socketfilterfw", "--getstealthmode");
                stealthMode = stealthOutput.Contains("enabled", StringComparison.OrdinalIgnoreCase)
                           && !stealthOutput.Contains("disabled", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // stealth mode query failed, leave as null
            }

            return new FirewallInfo(isEnabled, stealthMode);
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
            var output = commandRunner.Run("fdesetup", "status");
            // Output: "FileVault is On." or "FileVault is Off."
            var isEnabled = output.Contains("On", StringComparison.OrdinalIgnoreCase);
            return new EncryptionInfo(isEnabled, isEnabled ? "FileVault" : null);
        }
        catch
        {
            return new EncryptionInfo(false, null);
        }
    }

    private static long? ParseMemoryToBytes(string value)
    {
        // Parse strings like "8 GB", "1536 MB", "16384 MB"
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        if (!double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)) return null;

        return parts[1].ToUpperInvariant() switch
        {
            "GB" => (long)(amount * 1024 * 1024 * 1024),
            "MB" => (long)(amount * 1024 * 1024),
            "KB" => (long)(amount * 1024),
            _ => null
        };
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
