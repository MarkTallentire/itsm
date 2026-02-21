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

        int? threadCount = null;
        int? speedMHz = null;
        var siblings = 0;
        foreach (var line in cpuInfo.Split('\n'))
        {
            if (line.StartsWith("siblings", StringComparison.OrdinalIgnoreCase) && siblings == 0)
            {
                if (int.TryParse(line.Split(':', 2).Last().Trim(), out var s))
                    siblings = s;
            }
            else if (line.StartsWith("cpu MHz", StringComparison.OrdinalIgnoreCase) && speedMHz == null)
            {
                if (double.TryParse(line.Split(':', 2).Last().Trim(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var mhz))
                    speedMHz = (int)mhz;
            }
        }
        threadCount = siblings > 0 ? siblings : coreCount;

        return new CpuInfo(brandString, coreCount, threadCount, architecture, speedMHz);
    }

    public MemoryInfo GetMemoryInformation()
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        var modules = new List<MemoryModule>();

        try
        {
            var dmidecode = commandRunner.Run("dmidecode", "-t memory");
            string? slotLabel = null, type = null, manufacturer = null, serial = null;
            long capacity = 0;
            int? speed = null;

            foreach (var line in dmidecode.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Locator:", StringComparison.OrdinalIgnoreCase))
                    slotLabel = trimmed.Split(':', 2).Last().Trim();
                else if (trimmed.StartsWith("Size:", StringComparison.OrdinalIgnoreCase))
                {
                    var val = trimmed.Split(':', 2).Last().Trim();
                    if (val.Contains("No Module", StringComparison.OrdinalIgnoreCase))
                    {
                        slotLabel = null; type = null; manufacturer = null; serial = null;
                        capacity = 0; speed = null;
                        continue;
                    }
                    var sizeParts = val.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (sizeParts.Length >= 2 && long.TryParse(sizeParts[0], out var mb))
                    {
                        capacity = sizeParts[1].Equals("GB", StringComparison.OrdinalIgnoreCase)
                            ? mb * 1024 * 1024 * 1024
                            : mb * 1024 * 1024;
                    }
                }
                else if (trimmed.StartsWith("Speed:", StringComparison.OrdinalIgnoreCase))
                {
                    var val = trimmed.Split(':', 2).Last().Trim().Split(' ')[0];
                    if (int.TryParse(val, out var mhz))
                        speed = mhz;
                }
                else if (trimmed.StartsWith("Type:", StringComparison.OrdinalIgnoreCase) && !trimmed.StartsWith("Type Detail", StringComparison.OrdinalIgnoreCase))
                    type = trimmed.Split(':', 2).Last().Trim();
                else if (trimmed.StartsWith("Manufacturer:", StringComparison.OrdinalIgnoreCase))
                    manufacturer = trimmed.Split(':', 2).Last().Trim();
                else if (trimmed.StartsWith("Serial Number:", StringComparison.OrdinalIgnoreCase))
                {
                    serial = trimmed.Split(':', 2).Last().Trim();
                    // End of a module block — emit if we have capacity
                    if (capacity > 0)
                    {
                        modules.Add(new MemoryModule(slotLabel, capacity, speed, type, manufacturer, serial));
                    }
                    slotLabel = null; type = null; manufacturer = null; serial = null;
                    capacity = 0; speed = null;
                }
            }
        }
        catch { /* dmidecode may require root or not be available */ }

        return new MemoryInfo(gcMemoryInfo.TotalAvailableMemoryBytes, modules);
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

            string? kernelVersion = null;
            try { kernelVersion = commandRunner.Run("uname", "-r").Trim(); } catch { }

            var arch = RuntimeInformation.OSArchitecture.ToString();

            return new OsInfo(
                prettyName ?? RuntimeInformation.OSDescription,
                versionId,
                buildId,
                KernelName: "Linux",
                KernelVersion: kernelVersion,
                Architecture: arch,
                InstallDate: null,
                LicenseKey: null);
        }
        catch
        {
            return new OsInfo(RuntimeInformation.OSDescription, null, null, null, null, null, null, null);
        }
    }

    public NetworkInfo GetNetworkInformation()
    {
        var interfaces = new List<NetworkInterfaceInfo>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;

            var mac = nic.GetPhysicalAddress().ToString();
            var ipProps = nic.GetIPProperties();
            var addresses = ipProps.UnicastAddresses
                .Select(a => a.Address.ToString())
                .ToList();

            var speedMbps = nic.Speed > 0 ? nic.Speed / 1_000_000 : (long?)null;

            var interfaceType = nic.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Ethernet => "Ethernet",
                NetworkInterfaceType.Wireless80211 => "WiFi",
                NetworkInterfaceType.Loopback => "Loopback",
                NetworkInterfaceType.Tunnel => "Tunnel",
                _ => nic.NetworkInterfaceType.ToString()
            };

            bool? isDhcp = null;
            try { isDhcp = ipProps.GetIPv4Properties()?.IsDhcpEnabled; } catch { }

            string? gateway = ipProps.GatewayAddresses
                .Select(g => g.Address.ToString())
                .FirstOrDefault();

            string? subnetMask = null;
            var ipv4Addr = ipProps.UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            if (ipv4Addr != null)
                subnetMask = ipv4Addr.IPv4Mask?.ToString();

            // WiFi-specific info
            string? wifiSsid = null;
            double? wifiFreqGHz = null;
            int? wifiSignalDbm = null;

            if (interfaceType == "WiFi")
            {
                try
                {
                    var iwOutput = commandRunner.Run("iwconfig", nic.Name);
                    foreach (var iwLine in iwOutput.Split('\n'))
                    {
                        var t = iwLine.Trim();
                        if (t.Contains("ESSID:", StringComparison.OrdinalIgnoreCase))
                        {
                            var idx = t.IndexOf("ESSID:", StringComparison.OrdinalIgnoreCase) + 6;
                            wifiSsid = t[idx..].Trim('"', ' ');
                        }
                        if (t.Contains("Frequency:", StringComparison.OrdinalIgnoreCase))
                        {
                            var idx = t.IndexOf("Frequency:", StringComparison.OrdinalIgnoreCase) + 10;
                            var freqStr = t[idx..].Split(' ')[0];
                            if (double.TryParse(freqStr, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var freq))
                                wifiFreqGHz = freq;
                        }
                        if (t.Contains("Signal level=", StringComparison.OrdinalIgnoreCase))
                        {
                            var idx = t.IndexOf("Signal level=", StringComparison.OrdinalIgnoreCase) + 13;
                            var sigStr = t[idx..].Split(' ')[0];
                            if (int.TryParse(sigStr, out var dbm))
                                wifiSignalDbm = dbm;
                        }
                    }
                }
                catch { /* iwconfig not available, try iw */ }

                if (wifiSsid == null)
                {
                    try
                    {
                        var iwInfo = commandRunner.Run("iw", $"dev {nic.Name} info");
                        foreach (var iwLine in iwInfo.Split('\n'))
                        {
                            var t = iwLine.Trim();
                            if (t.StartsWith("ssid ", StringComparison.OrdinalIgnoreCase))
                                wifiSsid = t[5..].Trim();
                            if (t.Contains("channel", StringComparison.OrdinalIgnoreCase) && t.Contains("MHz"))
                            {
                                // Format: "channel 36 (5180 MHz)"
                                var mhzIdx = t.IndexOf('(');
                                var mhzEnd = t.IndexOf(" MHz", StringComparison.OrdinalIgnoreCase);
                                if (mhzIdx >= 0 && mhzEnd > mhzIdx)
                                {
                                    var mhzStr = t[(mhzIdx + 1)..mhzEnd].Trim();
                                    if (double.TryParse(mhzStr, out var mhz))
                                        wifiFreqGHz = mhz / 1000.0;
                                }
                            }
                        }
                    }
                    catch { /* iw not available */ }
                }
            }

            interfaces.Add(new NetworkInterfaceInfo(nic.Name, mac, addresses, speedMbps, interfaceType,
                isDhcp, gateway, subnetMask, wifiSsid, wifiFreqGHz, wifiSignalDbm));
        }

        // VPN connections
        var vpns = new List<VpnConnection>();
        try
        {
            var nmcli = commandRunner.Run("nmcli", "-t -f NAME,TYPE,STATE connection show");
            foreach (var line in nmcli.Split('\n'))
            {
                var parts = line.Trim().Split(':');
                if (parts.Length >= 3 && parts[1].Contains("vpn", StringComparison.OrdinalIgnoreCase))
                {
                    var connected = parts[2].Contains("activated", StringComparison.OrdinalIgnoreCase);
                    vpns.Add(new VpnConnection(parts[0], parts[1], null, connected));
                }
            }
        }
        catch { }

        // DNS configuration
        var dnsServers = new List<string>();
        string? dnsDomain = null;
        var searchDomains = new List<string>();
        try
        {
            var resolv = commandRunner.Run("cat", "/etc/resolv.conf");
            foreach (var line in resolv.Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith("nameserver ", StringComparison.OrdinalIgnoreCase))
                    dnsServers.Add(t[11..].Trim());
                else if (t.StartsWith("domain ", StringComparison.OrdinalIgnoreCase))
                    dnsDomain = t[7..].Trim();
                else if (t.StartsWith("search ", StringComparison.OrdinalIgnoreCase))
                    searchDomains.AddRange(t[7..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }
        }
        catch { }
        var dns = new DnsConfiguration(dnsServers, dnsDomain, searchDomains);

        // Network drives (NFS, CIFS/SMB mounts)
        var networkDrives = new List<NetworkDrive>();
        try
        {
            var mounts = commandRunner.Run("mount", "-t nfs,nfs4,cifs,smb");
            foreach (var line in mounts.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(" on ", 2, StringSplitOptions.None);
                if (parts.Length >= 2)
                {
                    var remote = parts[0].Trim();
                    var rest = parts[1].Split(" type ", 2);
                    var local = rest[0].Trim();
                    var fs = rest.Length > 1 ? rest[1].Split(' ')[0].Trim() : null;
                    networkDrives.Add(new NetworkDrive(local, remote, fs));
                }
            }
        }
        catch { }

        // Listening ports
        var ports = new List<ListeningPort>();
        try
        {
            var ss = commandRunner.Run("ss", "-tlnp");
            foreach (var line in ss.Split('\n'))
            {
                if (!line.Contains("LISTEN")) continue;
                if (line.StartsWith("State")) continue;
                var cols = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length >= 4)
                {
                    var localAddr = cols[3];
                    var lastColon = localAddr.LastIndexOf(':');
                    if (lastColon >= 0 && int.TryParse(localAddr[(lastColon + 1)..], out var port))
                    {
                        string? processName = null;
                        int? pid = null;
                        if (cols.Length >= 6)
                        {
                            var procInfo = cols[5];
                            var pidIdx = procInfo.IndexOf("pid=", StringComparison.OrdinalIgnoreCase);
                            if (pidIdx >= 0)
                            {
                                var pidStr = procInfo[(pidIdx + 4)..].Split(',')[0];
                                if (int.TryParse(pidStr, out var p)) pid = p;
                            }
                            var nameStart = procInfo.IndexOf('"');
                            var nameEnd = nameStart >= 0 ? procInfo.IndexOf('"', nameStart + 1) : -1;
                            if (nameStart >= 0 && nameEnd > nameStart)
                                processName = procInfo[(nameStart + 1)..nameEnd];
                        }
                        if (!ports.Any(p => p.Port == port && p.Protocol == "TCP"))
                            ports.Add(new ListeningPort(port, "TCP", processName, pid));
                    }
                }
            }
        }
        catch { }

        return new NetworkInfo(Environment.MachineName, interfaces, vpns, dns, networkDrives, ports);
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

        // Try dpkg (Debian/Ubuntu) — include Maintainer as publisher
        try
        {
            var dpkg = commandRunner.Run("dpkg-query", "-W -f '${Package}\\t${Version}\\t${Maintainer}\\n'");
            foreach (var line in dpkg.Split('\n'))
            {
                var trimmed = line.Trim().Trim('\'');
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                var parts = trimmed.Split('\t', 3);
                var name = parts[0].Trim();
                var version = parts.Length >= 2 ? parts[1].Trim() : "Unknown";
                var publisher = parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : null;
                if (!string.IsNullOrWhiteSpace(name))
                    apps.Add(new InstalledApp(name, version, null, publisher));
            }
        }
        catch { /* dpkg not available, try rpm */ }

        if (apps.Count == 0)
        {
            try
            {
                var rpm = commandRunner.Run("rpm", "-qa --queryformat '%{NAME}\\t%{VERSION}\\t%{VENDOR}\\n'");
                foreach (var line in rpm.Split('\n'))
                {
                    var trimmed = line.Trim().Trim('\'');
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    var parts = trimmed.Split('\t', 3);
                    var name = parts[0].Trim();
                    var version = parts.Length >= 2 ? parts[1].Trim() : "Unknown";
                    var publisher = parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : null;
                    if (!string.IsNullOrWhiteSpace(name))
                        apps.Add(new InstalledApp(name, version, null, publisher));
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

    public BiosInfo GetBiosInformation()
    {
        try
        {
            var vendor = commandRunner.Run("cat", "/sys/class/dmi/id/bios_vendor").Trim();
            var version = commandRunner.Run("cat", "/sys/class/dmi/id/bios_version").Trim();
            var date = commandRunner.Run("cat", "/sys/class/dmi/id/bios_date").Trim();
            string? serial = null;
            try { serial = commandRunner.Run("dmidecode", "-s bios-revision").Trim(); } catch { }

            return new BiosInfo(
                string.IsNullOrWhiteSpace(vendor) ? null : vendor,
                string.IsNullOrWhiteSpace(version) ? null : version,
                string.IsNullOrWhiteSpace(date) ? null : date,
                serial);
        }
        catch
        {
            return new BiosInfo(null, null, null, null);
        }
    }

    public MotherboardInfo GetMotherboardInformation()
    {
        try
        {
            var manufacturer = commandRunner.Run("cat", "/sys/class/dmi/id/board_vendor").Trim();
            var product = commandRunner.Run("cat", "/sys/class/dmi/id/board_name").Trim();
            var serial = commandRunner.Run("cat", "/sys/class/dmi/id/board_serial").Trim();
            var version = commandRunner.Run("cat", "/sys/class/dmi/id/board_version").Trim();

            return new MotherboardInfo(
                string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer,
                string.IsNullOrWhiteSpace(product) ? null : product,
                string.IsNullOrWhiteSpace(serial) ? null : serial,
                string.IsNullOrWhiteSpace(version) ? null : version);
        }
        catch
        {
            return new MotherboardInfo(null, null, null, null);
        }
    }

    public List<AntivirusInfo> GetAntivirusInformation()
    {
        var result = new List<AntivirusInfo>();

        // Check ClamAV
        try
        {
            var clamd = commandRunner.Run("clamdscan", "--version");
            if (!string.IsNullOrWhiteSpace(clamd))
            {
                var version = clamd.Trim().Split('/').FirstOrDefault()?.Replace("ClamAV", "").Trim();
                result.Add(new AntivirusInfo("ClamAV", version, true, null, null));
            }
        }
        catch { /* ClamAV not installed */ }

        return result;
    }

    public List<SystemController> GetControllers()
    {
        var controllers = new List<SystemController>();
        try
        {
            var lspci = commandRunner.Run("lspci", "-mm");
            foreach (var line in lspci.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                // lspci -mm format: Slot "Class" "Vendor" "Device" ...
                var parts = line.Split('"');
                if (parts.Length >= 6)
                {
                    var pciId = parts[0].Trim();
                    var deviceClass = parts[1];
                    var vendor = parts[3];
                    var device = parts[5];

                    // Skip VGA/3D controllers (already in GPUs)
                    if (deviceClass.Contains("VGA", StringComparison.OrdinalIgnoreCase) ||
                        deviceClass.Contains("3D", StringComparison.OrdinalIgnoreCase))
                        continue;

                    controllers.Add(new SystemController(device, vendor, deviceClass, pciId));
                }
            }
        }
        catch { /* lspci not available */ }

        return controllers;
    }

    public VirtualizationInfo GetVirtualizationInformation()
    {
        var vms = new List<VmInstance>();
        var containers = new List<DockerContainer>();

        // Docker containers
        try
        {
            var docker = commandRunner.Run("docker", "ps -a --format '{{.ID}}\\t{{.Names}}\\t{{.Image}}\\t{{.State}}\\t{{.Status}}'");
            foreach (var line in docker.Split('\n'))
            {
                var trimmed = line.Trim().Trim('\'');
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                var parts = trimmed.Split('\t');
                if (parts.Length >= 4)
                {
                    containers.Add(new DockerContainer(
                        parts[0], parts[1], parts[2], parts[3],
                        parts.Length >= 5 ? parts[4] : null));
                }
            }
        }
        catch { }

        // KVM/QEMU VMs via virsh
        try
        {
            var virsh = commandRunner.Run("virsh", "list --all --name");
            foreach (var line in virsh.Split('\n'))
            {
                var name = line.Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;
                string? state = null;
                try
                {
                    var info = commandRunner.Run("virsh", $"domstate {name}");
                    state = info.Trim();
                }
                catch { }
                vms.Add(new VmInstance(name, state, "KVM", null, null));
            }
        }
        catch { }

        return new VirtualizationInfo(vms, containers);
    }

    public List<DatabaseInstanceInfo> GetDatabaseInstances()
    {
        var dbs = new List<DatabaseInstanceInfo>();

        // PostgreSQL
        try
        {
            var pg = commandRunner.Run("pg_isready", "");
            var running = pg.Contains("accepting connections", StringComparison.OrdinalIgnoreCase);
            string? version = null;
            try { version = commandRunner.Run("psql", "--version").Trim().Split('\n')[0]; } catch { }
            dbs.Add(new DatabaseInstanceInfo("PostgreSQL", version, 5432, running));
        }
        catch { }

        // MySQL/MariaDB
        try
        {
            var mysql = commandRunner.Run("mysqladmin", "ping");
            var running = mysql.Contains("alive", StringComparison.OrdinalIgnoreCase);
            string? version = null;
            try { version = commandRunner.Run("mysql", "--version").Trim(); } catch { }
            dbs.Add(new DatabaseInstanceInfo("MySQL", version, 3306, running));
        }
        catch { }

        // MongoDB
        try
        {
            var mongo = commandRunner.Run("mongosh", "--eval 'db.version()' --quiet");
            if (!string.IsNullOrWhiteSpace(mongo))
                dbs.Add(new DatabaseInstanceInfo("MongoDB", mongo.Trim(), 27017, true));
        }
        catch { }

        // Redis
        try
        {
            var redis = commandRunner.Run("redis-cli", "ping");
            if (redis.Trim().Equals("PONG", StringComparison.OrdinalIgnoreCase))
            {
                string? version = null;
                try
                {
                    var info = commandRunner.Run("redis-cli", "info server");
                    foreach (var l in info.Split('\n'))
                        if (l.StartsWith("redis_version:")) { version = l.Split(':').Last().Trim(); break; }
                }
                catch { }
                dbs.Add(new DatabaseInstanceInfo("Redis", version, 6379, true));
            }
        }
        catch { }

        return dbs;
    }

    public async Task<LocationInfo?> GetLocationAsync()
    {
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = await http.GetStringAsync("http://ip-api.com/json/?fields=lat,lon,city,regionName,country,timezone,query");
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new LocationInfo(
                root.TryGetProperty("lat", out var lat) ? lat.GetDouble() : null,
                root.TryGetProperty("lon", out var lon) ? lon.GetDouble() : null,
                root.TryGetProperty("city", out var city) ? city.GetString() : null,
                root.TryGetProperty("regionName", out var region) ? region.GetString() : null,
                root.TryGetProperty("country", out var country) ? country.GetString() : null,
                root.TryGetProperty("timezone", out var tz) ? tz.GetString() : null,
                root.TryGetProperty("query", out var ip) ? ip.GetString() : null);
        }
        catch
        {
            return null;
        }
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
