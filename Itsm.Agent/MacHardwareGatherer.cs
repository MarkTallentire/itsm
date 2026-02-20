using System.Globalization;
using System.Net.Http;
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
        var brandString = commandRunner.Run("sysctl", "-n machdep.cpu.brand_string").Trim();

        int? threadCount = null;
        int? speedMHz = null;
        try
        {
            var tc = commandRunner.Run("sysctl", "-n hw.logicalcpu").Trim();
            if (int.TryParse(tc, out var t)) threadCount = t;
        }
        catch { }
        try
        {
            var cc = commandRunner.Run("sysctl", "-n hw.physicalcpu").Trim();
            if (int.TryParse(cc, out var c)) coreCount = c;
        }
        catch { }
        try
        {
            var freq = commandRunner.Run("sysctl", "-n hw.cpufrequency").Trim();
            if (long.TryParse(freq, out var hz)) speedMHz = (int)(hz / 1_000_000);
        }
        catch { }

        return new CpuInfo(brandString, coreCount, threadCount, architecture, speedMHz);
    }

    public MemoryInfo GetMemoryInformation()
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        var modules = new List<MemoryModule>();

        try
        {
            var output = commandRunner.Run("system_profiler", "SPMemoryDataType");
            string? slotLabel = null, type = null, manufacturer = null, serial = null;
            long capacity = 0;
            int? speed = null;

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("BANK", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("DIMM", StringComparison.OrdinalIgnoreCase))
                {
                    if (capacity > 0)
                        modules.Add(new MemoryModule(slotLabel, capacity, speed, type, manufacturer, serial));
                    slotLabel = trimmed.TrimEnd(':');
                    capacity = 0; speed = null; type = null; manufacturer = null; serial = null;
                }
                else if (trimmed.StartsWith("Size:", StringComparison.OrdinalIgnoreCase))
                {
                    var val = trimmed.Split(':', 2).Last().Trim();
                    var bytes = ParseMemoryToBytes(val);
                    if (bytes.HasValue) capacity = bytes.Value;
                }
                else if (trimmed.StartsWith("Speed:", StringComparison.OrdinalIgnoreCase))
                {
                    var val = trimmed.Split(':', 2).Last().Trim().Split(' ')[0];
                    if (int.TryParse(val, out var mhz)) speed = mhz;
                }
                else if (trimmed.StartsWith("Type:", StringComparison.OrdinalIgnoreCase))
                    type = trimmed.Split(':', 2).Last().Trim();
                else if (trimmed.StartsWith("Manufacturer:", StringComparison.OrdinalIgnoreCase))
                    manufacturer = trimmed.Split(':', 2).Last().Trim();
                else if (trimmed.StartsWith("Serial Number:", StringComparison.OrdinalIgnoreCase))
                    serial = trimmed.Split(':', 2).Last().Trim();
            }
            if (capacity > 0)
                modules.Add(new MemoryModule(slotLabel, capacity, speed, type, manufacturer, serial));
        }
        catch { }

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

            var mount = drive.Name.TrimEnd('/');
            if (mount != "" && mount != "/System/Volumes/Data") continue;

            disks.Add(new DiskInfo(drive.Name, drive.DriveFormat, drive.TotalSize, drive.AvailableFreeSpace));
        }

        if (disks.Count > 1 && disks.Any(d => d.Name.TrimEnd('/') == ""))
            disks.RemoveAll(d => d.Name.TrimEnd('/') == "/System/Volumes/Data");

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

            string? kernelVersion = null;
            try { kernelVersion = commandRunner.Run("uname", "-r").Trim(); } catch { }

            return new OsInfo(description, productVersion, buildVersion,
                KernelName: "Darwin",
                KernelVersion: kernelVersion,
                Architecture: RuntimeInformation.OSArchitecture.ToString(),
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

            // WiFi info via airport command
            string? wifiSsid = null;
            double? wifiFreqGHz = null;
            int? wifiSignalDbm = null;

            if (interfaceType == "WiFi")
            {
                try
                {
                    var airport = commandRunner.Run(
                        "/System/Library/PrivateFrameworks/Apple80211.framework/Versions/Current/Resources/airport",
                        "-I");
                    foreach (var airLine in airport.Split('\n'))
                    {
                        var t = airLine.Trim();
                        if (t.StartsWith("SSID:", StringComparison.OrdinalIgnoreCase))
                            wifiSsid = t.Split(':', 2).Last().Trim();
                        else if (t.StartsWith("channel:", StringComparison.OrdinalIgnoreCase))
                        {
                            // Format: "channel: 36" or "channel: 36,1"
                            var chStr = t.Split(':', 2).Last().Trim().Split(',')[0];
                            if (int.TryParse(chStr, out var ch))
                                wifiFreqGHz = ch <= 14 ? 2.4 : ch <= 64 ? 5.0 : 6.0;
                        }
                        else if (t.StartsWith("agrCtlRSSI:", StringComparison.OrdinalIgnoreCase))
                        {
                            var rssiStr = t.Split(':', 2).Last().Trim();
                            if (int.TryParse(rssiStr, out var rssi))
                                wifiSignalDbm = rssi;
                        }
                    }
                }
                catch { }
            }

            interfaces.Add(new NetworkInterfaceInfo(nic.Name, mac, addresses, speedMbps, interfaceType,
                isDhcp, gateway, subnetMask, wifiSsid, wifiFreqGHz, wifiSignalDbm));
        }

        // VPN connections
        var vpns = new List<VpnConnection>();
        try
        {
            var scutil = commandRunner.Run("scutil", "--nc list");
            foreach (var line in scutil.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var connected = line.Contains("Connected", StringComparison.OrdinalIgnoreCase);
                // Format: * (Connected)    GUID "VPN Name" [type]
                var quoteStart = line.IndexOf('"');
                var quoteEnd = line.LastIndexOf('"');
                if (quoteStart >= 0 && quoteEnd > quoteStart)
                {
                    var vpnName = line[(quoteStart + 1)..quoteEnd];
                    var bracketStart = line.IndexOf('[', quoteEnd);
                    var bracketEnd = line.IndexOf(']', quoteEnd);
                    var vpnType = bracketStart >= 0 && bracketEnd > bracketStart
                        ? line[(bracketStart + 1)..bracketEnd]
                        : null;
                    vpns.Add(new VpnConnection(vpnName, vpnType, null, connected));
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
            var scutilDns = commandRunner.Run("scutil", "--dns");
            foreach (var line in scutilDns.Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith("nameserver[", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0)
                    {
                        var server = t[(idx + 1)..].Trim();
                        if (!dnsServers.Contains(server)) dnsServers.Add(server);
                    }
                }
                else if (t.StartsWith("domain", StringComparison.OrdinalIgnoreCase) && dnsDomain == null)
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0) dnsDomain = t[(idx + 1)..].Trim();
                }
                else if (t.StartsWith("search domain[", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0) searchDomains.Add(t[(idx + 1)..].Trim());
                }
            }
        }
        catch { }
        var dns = new DnsConfiguration(dnsServers, dnsDomain, searchDomains);

        // Network drives (SMB/NFS mounts)
        var networkDrives = new List<NetworkDrive>();
        try
        {
            var mounts = commandRunner.Run("mount", "-t smbfs,nfs");
            foreach (var line in mounts.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(" on ", 2);
                if (parts.Length >= 2)
                {
                    var remote = parts[0].Trim();
                    var rest = parts[1].Split(" (", 2);
                    var local = rest[0].Trim();
                    var fs = rest.Length > 1 ? rest[1].Split(',')[0].Trim() : null;
                    networkDrives.Add(new NetworkDrive(local, remote, fs));
                }
            }
        }
        catch { }

        // Listening ports via lsof
        var ports = new List<ListeningPort>();
        try
        {
            var lsof = commandRunner.Run("lsof", "-iTCP -sTCP:LISTEN -nP");
            foreach (var line in lsof.Split('\n'))
            {
                if (line.StartsWith("COMMAND")) continue;
                var cols = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length >= 9)
                {
                    var processName = cols[0];
                    int? pid = int.TryParse(cols[1], out var p) ? p : null;
                    var addrField = cols[8];
                    var lastColon = addrField.LastIndexOf(':');
                    if (lastColon >= 0 && int.TryParse(addrField[(lastColon + 1)..], out var port))
                    {
                        if (!ports.Any(lp => lp.Port == port && lp.Protocol == "TCP"))
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
                    if (currentName != null)
                        gpus.Add(new GpuInfo(currentName, currentVendor ?? "Unknown", currentVram, null));
                    currentName = trimmed.Split(':', 2).Last().Trim();
                    currentVendor = null;
                    currentVram = null;
                }
                else if (trimmed.StartsWith("Vendor:"))
                    currentVendor = trimmed.Split(':', 2).Last().Trim();
                else if (trimmed.StartsWith("VRAM") || trimmed.StartsWith("Total Number of Cores:"))
                {
                    var value = trimmed.Split(':', 2).Last().Trim();
                    currentVram = ParseMemoryToBytes(value);
                }
            }

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
                    condition = trimmed.Split(':').Last().Trim();
                else if (trimmed.StartsWith("Maximum Capacity:"))
                {
                    var pctStr = trimmed.Split(':').Last().Trim().TrimEnd('%');
                    if (double.TryParse(pctStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
                        maxCapacity = pct;
                }
            }

            double? chargePercent = null;
            bool? isCharging = null;
            try
            {
                var pmsetOutput = commandRunner.Run("pmset", "-g batt");
                foreach (var line in pmsetOutput.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (!trimmed.Contains("InternalBattery")) continue;

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
                                isCharging = true;
                            else if (seg.Equals("discharging", StringComparison.OrdinalIgnoreCase)
                                  || seg.Equals("not charging", StringComparison.OrdinalIgnoreCase))
                                isCharging = false;
                            else if (seg.Equals("charged", StringComparison.OrdinalIgnoreCase)
                                  || seg.Equals("finishing charge", StringComparison.OrdinalIgnoreCase)
                                  || seg.Contains("AC attached", StringComparison.OrdinalIgnoreCase))
                                isCharging = false;
                        }
                    }
                }
            }
            catch { }

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

                    var publisher = app.TryGetProperty("obtained_from", out var pub) ? pub.GetString() : null;

                    apps.Add(new InstalledApp(name, version, installDate, publisher));
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
            catch { }

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
            var isEnabled = output.Contains("On", StringComparison.OrdinalIgnoreCase);
            return new EncryptionInfo(isEnabled, isEnabled ? "FileVault" : null);
        }
        catch
        {
            return new EncryptionInfo(false, null);
        }
    }

    public BiosInfo GetBiosInformation()
    {
        try
        {
            var output = commandRunner.Run("system_profiler", "SPHardwareDataType");
            string? bootRomVersion = null;

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("System Firmware Version:", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("Boot ROM Version:", StringComparison.OrdinalIgnoreCase))
                {
                    bootRomVersion = trimmed.Split(':', 2).Last().Trim();
                }
            }

            return new BiosInfo("Apple", bootRomVersion, null, null);
        }
        catch
        {
            return new BiosInfo(null, null, null, null);
        }
    }

    public MotherboardInfo GetMotherboardInformation()
    {
        // Apple Silicon and Intel Macs don't expose traditional motherboard info
        return new MotherboardInfo("Apple", null, null, null);
    }

    public List<AntivirusInfo> GetAntivirusInformation()
    {
        var result = new List<AntivirusInfo>();

        // Check XProtect (built-in macOS malware protection)
        try
        {
            var output = commandRunner.Run("system_profiler", "SPInstallHistoryDataType");
            if (output.Contains("XProtect", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new AntivirusInfo("XProtect", null, true, null, null));
            }
        }
        catch { }

        return result;
    }

    public List<SystemController> GetControllers()
    {
        var controllers = new List<SystemController>();
        try
        {
            var output = commandRunner.Run("system_profiler", "SPUSBDataType SPThunderboltDataType");
            string? currentName = null;
            string? currentManufacturer = null;
            string? currentType = null;

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.EndsWith(':') && !trimmed.StartsWith("USB") && !trimmed.StartsWith("Thunderbolt"))
                {
                    if (currentName != null)
                        controllers.Add(new SystemController(currentName, currentManufacturer, currentType, null));
                    currentName = trimmed.TrimEnd(':');
                    currentManufacturer = null;
                    currentType = line.Contains("Thunderbolt") ? "Thunderbolt" : "USB";
                }
                else if (trimmed.StartsWith("Manufacturer:", StringComparison.OrdinalIgnoreCase))
                    currentManufacturer = trimmed.Split(':', 2).Last().Trim();
            }
            if (currentName != null)
                controllers.Add(new SystemController(currentName, currentManufacturer, currentType, null));
        }
        catch { }

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

        // MySQL
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
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = await http.GetStringAsync("http://ip-api.com/json/?fields=lat,lon,city,regionName,country,timezone,query");
            using var doc = JsonDocument.Parse(json);
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

    private static long? ParseMemoryToBytes(string value)
    {
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
