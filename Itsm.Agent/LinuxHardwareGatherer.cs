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
            if (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable) continue;
            if (drive.TotalSize == 0) continue;

            // Skip pseudo/virtual filesystems
            var format = drive.DriveFormat.ToLowerInvariant();
            if (format is "tmpfs" or "devtmpfs" or "sysfs" or "proc" or "devpts" or "cgroup" or "cgroup2"
                or "overlay" or "squashfs" or "fuse.snapfuse") continue;

            disks.Add(new DiskInfo(drive.Name, drive.DriveFormat, drive.TotalSize, drive.AvailableFreeSpace));
        }
        return disks;
    }

    public OsInfo GetOsInformation()
    {
        try
        {
            var osRelease = commandRunner.Run("cat", "/etc/os-release");
            string? prettyName = null, versionId = null, buildId = null;

            foreach (var line in osRelease.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("PRETTY_NAME=", StringComparison.OrdinalIgnoreCase))
                    prettyName = trimmed.Split('=', 2).Last().Trim('"');
                else if (trimmed.StartsWith("VERSION_ID=", StringComparison.OrdinalIgnoreCase))
                    versionId = trimmed.Split('=', 2).Last().Trim('"');
                else if (trimmed.StartsWith("BUILD_ID=", StringComparison.OrdinalIgnoreCase))
                    buildId = trimmed.Split('=', 2).Last().Trim('"');
            }

            return new OsInfo(
                prettyName ?? RuntimeInformation.OSDescription,
                versionId,
                buildId);
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

        var modelName = commandRunner.Run("cat", "/sys/class/dmi/id/product_name");
        var serialNumber = commandRunner.Run("cat", "/sys/class/dmi/id/product_serial");
        var hardwareUuid = commandRunner.Run("cat", "/sys/class/dmi/id/product_uuid");
        var chassisCode = commandRunner.Run("cat", "/sys/class/dmi/id/chassis_type");
        var chassisType = ParseChassisType(chassisCode);

        return new MachineIdentity(computerName, modelName, serialNumber, hardwareUuid, loggedInUser, chassisType);
    }

    public List<GpuInfo> GetGpuInformation()
    {
        var gpus = new List<GpuInfo>();
        try
        {
            var lspci = commandRunner.Run("lspci", "");
            foreach (var line in lspci.Split('\n'))
            {
                if (!line.Contains("VGA", StringComparison.OrdinalIgnoreCase) &&
                    !line.Contains("3D controller", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Line format: "00:02.0 VGA compatible controller: Intel Corporation ..."
                var parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
                var name = parts.Length > 1 ? parts[1].Trim() : line.Trim();
                var vendor = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ? "NVIDIA"
                    : name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("ATI", StringComparison.OrdinalIgnoreCase) ? "AMD"
                    : name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? "Intel"
                    : "Unknown";

                long? vramBytes = null;
                string? driverVersion = null;

                if (vendor == "NVIDIA")
                {
                    try
                    {
                        var smi = commandRunner.Run("nvidia-smi",
                            "--query-gpu=memory.total,driver_version --format=csv,noheader,nounits");
                        var smiLine = smi.Split('\n').FirstOrDefault()?.Trim();
                        if (!string.IsNullOrEmpty(smiLine))
                        {
                            var smiParts = smiLine.Split(',');
                            if (smiParts.Length >= 1 && long.TryParse(smiParts[0].Trim(), out var mb))
                                vramBytes = mb * 1024 * 1024;
                            if (smiParts.Length >= 2)
                                driverVersion = smiParts[1].Trim();
                        }
                    }
                    catch { /* nvidia-smi not available */ }
                }

                gpus.Add(new GpuInfo(name, vendor, vramBytes, driverVersion));
            }
        }
        catch { /* lspci not available */ }

        return gpus;
    }

    public BatteryInfo GetBatteryInformation()
    {
        const string batteryPath = "/sys/class/power_supply/BAT0";
        try
        {
            var capacityStr = commandRunner.Run("cat", $"{batteryPath}/capacity");
            if (string.IsNullOrWhiteSpace(capacityStr))
                return new BatteryInfo(false, null, null, null, null, null);

            double.TryParse(capacityStr.Trim(), out var chargePercent);

            int? cycleCount = null;
            try
            {
                var cycleStr = commandRunner.Run("cat", $"{batteryPath}/cycle_count");
                if (int.TryParse(cycleStr.Trim(), out var cc))
                    cycleCount = cc;
            }
            catch { /* cycle_count not available on all systems */ }

            var status = commandRunner.Run("cat", $"{batteryPath}/status").Trim();
            var isCharging = status.Equals("Charging", StringComparison.OrdinalIgnoreCase);

            double? healthPercent = null;
            try
            {
                var fullStr = commandRunner.Run("cat", $"{batteryPath}/charge_full");
                var designStr = commandRunner.Run("cat", $"{batteryPath}/charge_full_design");
                if (long.TryParse(fullStr.Trim(), out var full) && long.TryParse(designStr.Trim(), out var design) && design > 0)
                    healthPercent = Math.Round((double)full / design * 100, 1);
            }
            catch
            {
                try
                {
                    var fullStr = commandRunner.Run("cat", $"{batteryPath}/energy_full");
                    var designStr = commandRunner.Run("cat", $"{batteryPath}/energy_full_design");
                    if (long.TryParse(fullStr.Trim(), out var full) && long.TryParse(designStr.Trim(), out var design) && design > 0)
                        healthPercent = Math.Round((double)full / design * 100, 1);
                }
                catch { /* energy files not available */ }
            }

            var condition = healthPercent switch
            {
                >= 80 => "Good",
                >= 50 => "Fair",
                _ => "Poor"
            };
            if (healthPercent == null) condition = null;

            return new BatteryInfo(true, chargePercent, cycleCount, healthPercent, isCharging, condition);
        }
        catch
        {
            return new BatteryInfo(false, null, null, null, null, null);
        }
    }

    public List<InstalledApp> GetInstalledApplications()
    {
        var apps = new List<InstalledApp>();

        // Try dpkg (Debian/Ubuntu)
        try
        {
            var dpkg = commandRunner.Run("dpkg-query", "-W -f '${Package}\\t${Version}\\n'");
            foreach (var line in dpkg.Split('\n'))
            {
                var trimmed = line.Trim().Trim('\'');
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                var parts = trimmed.Split('\t', 2);
                if (parts.Length >= 2)
                    apps.Add(new InstalledApp(parts[0].Trim(), parts[1].Trim(), null));
                else if (parts.Length == 1 && !string.IsNullOrWhiteSpace(parts[0]))
                    apps.Add(new InstalledApp(parts[0].Trim(), "Unknown", null));
            }
        }
        catch { /* dpkg not available, try rpm */ }

        if (apps.Count == 0)
        {
            try
            {
                var rpm = commandRunner.Run("rpm", "-qa --queryformat '%{NAME}\\t%{VERSION}\\n'");
                foreach (var line in rpm.Split('\n'))
                {
                    var trimmed = line.Trim().Trim('\'');
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    var parts = trimmed.Split('\t', 2);
                    if (parts.Length >= 2)
                        apps.Add(new InstalledApp(parts[0].Trim(), parts[1].Trim(), null));
                    else if (parts.Length == 1 && !string.IsNullOrWhiteSpace(parts[0]))
                        apps.Add(new InstalledApp(parts[0].Trim(), "Unknown", null));
                }
            }
            catch { /* rpm not available either */ }
        }

        return apps;
    }

    public UptimeInfo GetUptimeInformation()
    {
        try
        {
            var uptime = commandRunner.Run("cat", "/proc/uptime");
            var parts = uptime.Split(' ');
            if (parts.Length >= 1 && double.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var seconds))
            {
                var uptimeSpan = TimeSpan.FromSeconds(seconds);
                var bootTime = DateTime.UtcNow - uptimeSpan;
                return new UptimeInfo(bootTime, uptimeSpan);
            }
        }
        catch { /* /proc/uptime not available */ }

        return new UptimeInfo(DateTime.UtcNow, TimeSpan.Zero);
    }

    public FirewallInfo GetFirewallInformation()
    {
        // Try ufw (Ubuntu/Debian)
        try
        {
            var ufw = commandRunner.Run("ufw", "status");
            if (!string.IsNullOrWhiteSpace(ufw))
            {
                var enabled = ufw.Contains("Status: active", StringComparison.OrdinalIgnoreCase);
                return new FirewallInfo(enabled, null);
            }
        }
        catch { /* ufw not available */ }

        // Try firewall-cmd (RHEL/Fedora/CentOS)
        try
        {
            var fwcmd = commandRunner.Run("firewall-cmd", "--state");
            if (!string.IsNullOrWhiteSpace(fwcmd))
            {
                var enabled = fwcmd.Trim().Equals("running", StringComparison.OrdinalIgnoreCase);
                return new FirewallInfo(enabled, null);
            }
        }
        catch { /* firewall-cmd not available */ }

        // Try iptables as last resort
        try
        {
            var iptables = commandRunner.Run("iptables", "-L -n");
            if (!string.IsNullOrWhiteSpace(iptables))
            {
                // If there are rules beyond the default ACCEPT chains, firewall is active
                var lines = iptables.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                var hasRules = lines.Count > 6; // 3 chains x 2 header lines = 6 minimum lines
                return new FirewallInfo(hasRules, null);
            }
        }
        catch { /* iptables not available */ }

        return new FirewallInfo(false, null);
    }

    public EncryptionInfo GetEncryptionInformation()
    {
        // Check for LUKS volumes
        try
        {
            var lsblk = commandRunner.Run("lsblk", "-o NAME,TYPE,FSTYPE -n");
            if (lsblk.Contains("crypto_LUKS", StringComparison.OrdinalIgnoreCase) ||
                lsblk.Contains("crypt", StringComparison.OrdinalIgnoreCase))
            {
                return new EncryptionInfo(true, "LUKS");
            }
        }
        catch { /* lsblk not available */ }

        // Check dmsetup for dm-crypt
        try
        {
            var dmsetup = commandRunner.Run("dmsetup", "status");
            if (!string.IsNullOrWhiteSpace(dmsetup) &&
                dmsetup.Contains("crypt", StringComparison.OrdinalIgnoreCase))
            {
                return new EncryptionInfo(true, "dm-crypt");
            }
        }
        catch { /* dmsetup not available */ }

        return new EncryptionInfo(false, null);
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
