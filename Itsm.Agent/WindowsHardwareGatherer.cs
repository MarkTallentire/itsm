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
        var output = commandRunner.Run("wmic", "cpu get Name,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed /format:list");
        var brandString = ParseWmicValue(output, "Name");
        var threadStr = ParseWmicValue(output, "NumberOfLogicalProcessors");
        var coresStr = ParseWmicValue(output, "NumberOfCores");
        var speedStr = ParseWmicValue(output, "MaxClockSpeed");

        int? threadCount = int.TryParse(threadStr, out var tc) ? tc : null;
        if (int.TryParse(coresStr, out var cores) && cores > 0)
            coreCount = cores;
        int? speedMHz = int.TryParse(speedStr, out var spd) ? spd : null;

        return new CpuInfo(brandString, coreCount, threadCount, architecture, speedMHz);
    }

    public MemoryInfo GetMemoryInformation()
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        var modules = new List<MemoryModule>();

        try
        {
            var output = commandRunner.Run("wmic", "memorychip get BankLabel,Capacity,Speed,MemoryType,Manufacturer,SerialNumber /format:list");
            var blocks = output.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var block in blocks)
            {
                var capStr = ParseWmicValue(block, "Capacity");
                if (!long.TryParse(capStr, out var cap) || cap == 0) continue;

                var slot = ParseWmicValue(block, "BankLabel");
                var speedStr = ParseWmicValue(block, "Speed");
                var typeStr = ParseWmicValue(block, "MemoryType");
                var mfr = ParseWmicValue(block, "Manufacturer");
                var serial = ParseWmicValue(block, "SerialNumber");

                int? speed = int.TryParse(speedStr, out var s) ? s : null;
                var memType = typeStr switch
                {
                    "24" => "DDR3",
                    "26" => "DDR4",
                    "34" => "DDR5",
                    _ => int.TryParse(typeStr, out _) ? $"Type {typeStr}" : typeStr
                };

                modules.Add(new MemoryModule(
                    slot != "Unknown" ? slot : null,
                    cap,
                    speed,
                    memType != "Unknown" ? memType : null,
                    mfr != "Unknown" ? mfr : null,
                    serial != "Unknown" ? serial : null));
            }
        }
        catch { /* wmic may not be available */ }

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
            disks.Add(new DiskInfo(drive.Name, drive.DriveFormat, drive.TotalSize, drive.AvailableFreeSpace));
        }
        return disks;
    }

    public OsInfo GetOsInformation()
    {
        try
        {
            var output = commandRunner.Run("wmic", "os get Caption,Version,BuildNumber,InstallDate,OSArchitecture /format:list");
            var caption = ParseWmicValue(output, "Caption");
            var version = ParseWmicValue(output, "Version");
            var buildNumber = ParseWmicValue(output, "BuildNumber");
            var installDate = ParseWmicValue(output, "InstallDate");
            var arch = ParseWmicValue(output, "OSArchitecture");

            string? licenseKey = null;
            try
            {
                var keyOutput = commandRunner.Run("wmic", "path SoftwareLicensingService get OA3xOriginalProductKey /format:value");
                var key = ParseWmicValue(keyOutput, "OA3xOriginalProductKey");
                if (key != "Unknown" && !string.IsNullOrWhiteSpace(key))
                    licenseKey = key;
            }
            catch { /* license key retrieval optional */ }

            return new OsInfo(
                caption != "Unknown" ? caption : RuntimeInformation.OSDescription,
                version != "Unknown" ? version : null,
                buildNumber != "Unknown" ? buildNumber : null,
                KernelName: "Windows NT",
                KernelVersion: version != "Unknown" ? version : null,
                Architecture: arch != "Unknown" ? arch : null,
                InstallDate: installDate != "Unknown" ? installDate : null,
                LicenseKey: licenseKey);
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

            // WiFi-specific info via netsh
            string? wifiSsid = null;
            double? wifiFreqGHz = null;
            int? wifiSignalDbm = null;

            if (interfaceType == "WiFi")
            {
                try
                {
                    var netsh = commandRunner.Run("netsh", $"wlan show interfaces");
                    foreach (var netshLine in netsh.Split('\n'))
                    {
                        var t = netshLine.Trim();
                        if (t.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) && !t.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase))
                        {
                            var idx = t.IndexOf(':');
                            if (idx >= 0) wifiSsid = t[(idx + 1)..].Trim();
                        }
                        else if (t.StartsWith("Channel", StringComparison.OrdinalIgnoreCase))
                        {
                            var idx = t.IndexOf(':');
                            if (idx >= 0 && int.TryParse(t[(idx + 1)..].Trim(), out var ch))
                            {
                                // Approximate frequency from channel number
                                wifiFreqGHz = ch <= 14 ? 2.4 : 5.0;
                            }
                        }
                        else if (t.StartsWith("Signal", StringComparison.OrdinalIgnoreCase))
                        {
                            var idx = t.IndexOf(':');
                            if (idx >= 0)
                            {
                                var pctStr = t[(idx + 1)..].Trim().TrimEnd('%');
                                if (int.TryParse(pctStr, out var pct))
                                    wifiSignalDbm = (pct / 2) - 100; // Approximate dBm from percentage
                            }
                        }
                    }
                }
                catch { /* netsh wlan not available */ }
            }

            interfaces.Add(new NetworkInterfaceInfo(nic.Name, mac, addresses, speedMbps, interfaceType,
                isDhcp, gateway, subnetMask, wifiSsid, wifiFreqGHz, wifiSignalDbm));
        }

        // VPN connections via rasdial
        var vpns = new List<VpnConnection>();
        try
        {
            var rasOutput = commandRunner.Run("powershell", "-Command \"Get-VpnConnection | Select-Object Name,ServerAddress,ConnectionStatus | Format-List\"");
            string? vpnName = null, serverAddr = null;
            foreach (var line in rasOutput.Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith("Name", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0) vpnName = t[(idx + 1)..].Trim();
                }
                else if (t.StartsWith("ServerAddress", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0) serverAddr = t[(idx + 1)..].Trim();
                }
                else if (t.StartsWith("ConnectionStatus", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0 && vpnName != null)
                    {
                        var status = t[(idx + 1)..].Trim();
                        vpns.Add(new VpnConnection(vpnName, "Windows VPN", serverAddr,
                            status.Equals("Connected", StringComparison.OrdinalIgnoreCase)));
                    }
                    vpnName = null; serverAddr = null;
                }
            }
        }
        catch { }

        // DNS configuration
        var dnsServers = new List<string>();
        string? dnsDomain = null;
        var searchDomains = new List<string>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            var props = nic.GetIPProperties();
            foreach (var dns in props.DnsAddresses)
                if (!dnsServers.Contains(dns.ToString()))
                    dnsServers.Add(dns.ToString());
            if (dnsDomain == null && !string.IsNullOrEmpty(props.DnsSuffix))
                dnsDomain = props.DnsSuffix;
        }
        var dnsConfig = new DnsConfiguration(dnsServers, dnsDomain, searchDomains);

        // Network drives (mapped drives)
        var networkDrives = new List<NetworkDrive>();
        try
        {
            var netUse = commandRunner.Run("net", "use");
            foreach (var line in netUse.Split('\n'))
            {
                var t = line.Trim();
                if (!t.StartsWith("OK", StringComparison.OrdinalIgnoreCase) &&
                    !t.StartsWith("Disconnected", StringComparison.OrdinalIgnoreCase) &&
                    !t.StartsWith("Unavailable", StringComparison.OrdinalIgnoreCase))
                    continue;
                var parts = t.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                    networkDrives.Add(new NetworkDrive(parts[1], parts[2], null));
            }
        }
        catch { }

        // Listening ports via netstat
        var ports = new List<ListeningPort>();
        try
        {
            var netstat = commandRunner.Run("netstat", "-ano -p TCP");
            foreach (var line in netstat.Split('\n'))
            {
                var t = line.Trim();
                if (!t.Contains("LISTENING", StringComparison.OrdinalIgnoreCase)) continue;
                var cols = t.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length >= 5)
                {
                    var localAddr = cols[1];
                    var lastColon = localAddr.LastIndexOf(':');
                    if (lastColon >= 0 && int.TryParse(localAddr[(lastColon + 1)..], out var port))
                    {
                        int? pid = int.TryParse(cols[^1], out var p) ? p : null;
                        if (!ports.Any(lp => lp.Port == port && lp.Protocol == "TCP"))
                            ports.Add(new ListeningPort(port, "TCP", null, pid));
                    }
                }
            }
        }
        catch { }

        return new NetworkInfo(Environment.MachineName, interfaces, vpns, dnsConfig, networkDrives, ports);
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

            var blocks = output.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var block in blocks)
            {
                var name = ParseWmicValue(block, "Name");
                if (name == "Unknown") continue;

                var ramStr = ParseWmicValue(block, "AdapterRAM");
                long? vramBytes = long.TryParse(ramStr, out var ram) ? ram : null;

                var driverVersion = ParseWmicValue(block, "DriverVersion");

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
            bool? isCharging = int.TryParse(statusStr, out var status) ? status == 2 : null;

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
            catch { }

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
            catch { }

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
        Select-Object DisplayName, DisplayVersion, InstallDate, Publisher |
        ForEach-Object { ""$($_.DisplayName)|$($_.DisplayVersion)|$($_.InstallDate)|$($_.Publisher)"" }
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
                var publisher = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3].Trim() : null;

                if (!apps.ContainsKey(name))
                    apps[name] = new InstalledApp(name, version, installDate, publisher);
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

            if (raw.Length >= 14)
            {
                var year = int.Parse(raw[..4]);
                var month = int.Parse(raw[4..6]);
                var day = int.Parse(raw[6..8]);
                var hour = int.Parse(raw[8..10]);
                var minute = int.Parse(raw[10..12]);
                var second = int.Parse(raw[12..14]);

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
            return new EncryptionInfo(false, null);
        }
    }

    public BiosInfo GetBiosInformation()
    {
        try
        {
            var output = commandRunner.Run("wmic", "bios get Manufacturer,SMBIOSBIOSVersion,ReleaseDate,SerialNumber /format:list");
            var manufacturer = ParseWmicValue(output, "Manufacturer");
            var version = ParseWmicValue(output, "SMBIOSBIOSVersion");
            var date = ParseWmicValue(output, "ReleaseDate");
            var serial = ParseWmicValue(output, "SerialNumber");

            return new BiosInfo(
                manufacturer != "Unknown" ? manufacturer : null,
                version != "Unknown" ? version : null,
                date != "Unknown" ? date : null,
                serial != "Unknown" ? serial : null);
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
            var output = commandRunner.Run("wmic", "baseboard get Manufacturer,Product,SerialNumber,Version /format:list");
            var manufacturer = ParseWmicValue(output, "Manufacturer");
            var product = ParseWmicValue(output, "Product");
            var serial = ParseWmicValue(output, "SerialNumber");
            var version = ParseWmicValue(output, "Version");

            return new MotherboardInfo(
                manufacturer != "Unknown" ? manufacturer : null,
                product != "Unknown" ? product : null,
                serial != "Unknown" ? serial : null,
                version != "Unknown" ? version : null);
        }
        catch
        {
            return new MotherboardInfo(null, null, null, null);
        }
    }

    public List<AntivirusInfo> GetAntivirusInformation()
    {
        var result = new List<AntivirusInfo>();
        try
        {
            // Windows Security Center exposes registered AV products
            var output = commandRunner.Run("powershell",
                "-Command \"Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct | Select-Object displayName,pathToSignedProductExe,productState | Format-List\"");

            string? name = null;
            int productState = 0;

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("displayName", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = trimmed.IndexOf(':');
                    if (idx >= 0) name = trimmed[(idx + 1)..].Trim();
                }
                else if (trimmed.StartsWith("productState", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = trimmed.IndexOf(':');
                    if (idx >= 0) int.TryParse(trimmed[(idx + 1)..].Trim(), out productState);

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        // productState is a bitmask: bits 12-8 = scanner state, bit 4 = definitions up to date
                        var isEnabled = ((productState >> 12) & 0xF) == 1;
                        var isUpToDate = ((productState >> 4) & 0x1) == 0;
                        result.Add(new AntivirusInfo(name, null, isEnabled, isUpToDate, null));
                    }
                    name = null;
                    productState = 0;
                }
            }
        }
        catch { /* SecurityCenter2 may not be available */ }

        return result;
    }

    public List<SystemController> GetControllers()
    {
        var controllers = new List<SystemController>();
        try
        {
            var output = commandRunner.Run("wmic", "path Win32_PnPEntity where \"PNPClass<>''\" get Name,Manufacturer,PNPClass,DeviceID /format:list");
            var blocks = output.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var block in blocks)
            {
                var name = ParseWmicValue(block, "Name");
                if (name == "Unknown") continue;
                var manufacturer = ParseWmicValue(block, "Manufacturer");
                var pnpClass = ParseWmicValue(block, "PNPClass");
                var deviceId = ParseWmicValue(block, "DeviceID");

                // Only include controller-type devices (skip display adapters, already in GPUs)
                if (pnpClass.Contains("Display", StringComparison.OrdinalIgnoreCase)) continue;

                // Filter to interesting device classes
                if (pnpClass is "System" or "USB" or "HDC" or "SCSIAdapter" or "Net" or "Media"
                    or "AudioEndpoint" or "Bluetooth" or "Biometric" or "Camera" or "Sensor")
                {
                    controllers.Add(new SystemController(
                        name,
                        manufacturer != "Unknown" ? manufacturer : null,
                        pnpClass,
                        deviceId != "Unknown" ? deviceId : null));
                }
            }
        }
        catch { /* wmic may not be available */ }

        return controllers;
    }

    public VirtualizationInfo GetVirtualizationInformation()
    {
        var vms = new List<VmInstance>();
        var containers = new List<DockerContainer>();

        // Docker containers
        try
        {
            var docker = commandRunner.Run("docker", "ps -a --format \"{{.ID}}\\t{{.Names}}\\t{{.Image}}\\t{{.State}}\\t{{.Status}}\"");
            foreach (var line in docker.Split('\n'))
            {
                var trimmed = line.Trim();
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

        // Hyper-V VMs
        try
        {
            var hyperv = commandRunner.Run("powershell", "-Command \"Get-VM | Select-Object Name,State,MemoryAssigned,ProcessorCount | Format-List\"");
            string? vmName = null, vmState = null;
            long? vmMem = null;
            int? vmCpu = null;

            foreach (var line in hyperv.Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith("Name", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0) vmName = t[(idx + 1)..].Trim();
                }
                else if (t.StartsWith("State", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0) vmState = t[(idx + 1)..].Trim();
                }
                else if (t.StartsWith("MemoryAssigned", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0 && long.TryParse(t[(idx + 1)..].Trim(), out var mem))
                        vmMem = mem / (1024 * 1024);
                }
                else if (t.StartsWith("ProcessorCount", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0 && int.TryParse(t[(idx + 1)..].Trim(), out var cpu))
                        vmCpu = cpu;

                    if (vmName != null)
                        vms.Add(new VmInstance(vmName, vmState, "Hyper-V", vmMem, vmCpu));
                    vmName = null; vmState = null; vmMem = null; vmCpu = null;
                }
            }
        }
        catch { }

        return new VirtualizationInfo(vms, containers);
    }

    public List<DatabaseInstanceInfo> GetDatabaseInstances()
    {
        var dbs = new List<DatabaseInstanceInfo>();

        // SQL Server
        try
        {
            var sqlServer = commandRunner.Run("powershell",
                "-Command \"Get-Service -Name 'MSSQLSERVER','MSSQL$*' -ErrorAction SilentlyContinue | Select-Object Name,Status | Format-List\"");
            string? svcName = null;
            foreach (var line in sqlServer.Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith("Name", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0) svcName = t[(idx + 1)..].Trim();
                }
                else if (t.StartsWith("Status", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = t.IndexOf(':');
                    if (idx >= 0 && svcName != null)
                    {
                        var running = t[(idx + 1)..].Trim().Equals("Running", StringComparison.OrdinalIgnoreCase);
                        dbs.Add(new DatabaseInstanceInfo(svcName, null, 1433, running));
                    }
                    svcName = null;
                }
            }
        }
        catch { }

        // PostgreSQL
        try
        {
            var pg = commandRunner.Run("pg_isready", "");
            var running = pg.Contains("accepting connections", StringComparison.OrdinalIgnoreCase);
            dbs.Add(new DatabaseInstanceInfo("PostgreSQL", null, 5432, running));
        }
        catch { }

        // MySQL
        try
        {
            var mysql = commandRunner.Run("mysqladmin", "ping");
            dbs.Add(new DatabaseInstanceInfo("MySQL", null, 3306,
                mysql.Contains("alive", StringComparison.OrdinalIgnoreCase)));
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
