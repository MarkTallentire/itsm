using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Itsm.Common.Models;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace Itsm.Agent;

public class NetworkPrinterScanner(ILogger<NetworkPrinterScanner> logger) : INetworkPrinterScanner
{
    private static readonly TimeSpan SnmpTimeout = TimeSpan.FromSeconds(3);

    // Standard printer MIB OIDs
    private static readonly ObjectIdentifier OidSysDescr = new("1.3.6.1.2.1.1.1.0");
    private static readonly ObjectIdentifier OidSysName = new("1.3.6.1.2.1.1.5.0");
    private static readonly ObjectIdentifier OidHrDeviceDescr = new("1.3.6.1.2.1.25.3.2.1.3.1");
    private static readonly ObjectIdentifier OidPrtGeneralSerial = new("1.3.6.1.2.1.43.5.1.1.17.1");
    private static readonly ObjectIdentifier OidPrtPageCount = new("1.3.6.1.2.1.43.10.2.1.4.1.1");

    // Toner OIDs — Printer MIB marker supplies table
    // Max capacity: 1.3.6.1.2.1.43.11.1.1.8.1.{index}
    // Current level: 1.3.6.1.2.1.43.11.1.1.9.1.{index}
    // Supply description: 1.3.6.1.2.1.43.11.1.1.6.1.{index}
    private const string OidSupplyDescBase = "1.3.6.1.2.1.43.11.1.1.6.1";
    private const string OidSupplyMaxBase = "1.3.6.1.2.1.43.11.1.1.8.1";
    private const string OidSupplyLevelBase = "1.3.6.1.2.1.43.11.1.1.9.1";

    public async Task<List<NetworkPrinterInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var printers = new List<NetworkPrinterInfo>();
        try
        {
            // Overall scan timeout — 5 minutes max
            using var scanCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            scanCts.CancelAfter(TimeSpan.FromMinutes(5));

            var subnets = GetLocalSubnets();
            logger.LogInformation("Found {Count} local subnets: {Subnets}", subnets.Count,
                string.Join(", ", subnets.Select(s => $"{s.Address}/{s.Mask}")));
            if (subnets.Count == 0)
            {
                logger.LogWarning("No local subnets found for printer scanning");
                return [];
            }

            foreach (var (address, mask) in subnets)
            {
                var hosts = GetSubnetHosts(address, mask);
                logger.LogInformation("Scanning {Count} hosts on {Subnet}/{Mask} for SNMP printers", hosts.Count, address, mask);

                // Scan in batches to avoid overwhelming the network
                const int batchSize = 20;
                for (var i = 0; i < hosts.Count; i += batchSize)
                {
                    var batch = hosts.Skip(i).Take(batchSize);
                    var tasks = batch.Select(ip => ScanHostAsync(ip, scanCts.Token)).ToList();
                    var results = await Task.WhenAll(tasks);

                    var found = results.Count(r => r != null);
                    if (found > 0)
                        logger.LogInformation("Batch {BatchStart}-{BatchEnd}: found {Count} printers", i, i + tasks.Count - 1, found);

                    foreach (var result in results)
                    {
                        if (result != null)
                            printers.Add(result);
                    }
                }
            }

            logger.LogInformation("Printer scan complete, found {Count} printers", printers.Count);
            return printers;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Overall scan timeout hit — return whatever we found so far
            logger.LogWarning("Printer scan timed out, returning {Count} printers found so far", printers.Count);
            return printers;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Printer scan failed, returning {Count} printers found so far", printers.Count);
            return printers;
        }
    }

    private async Task<NetworkPrinterInfo?> ScanHostAsync(IPAddress ip, CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = new IPEndPoint(ip, 161);
            var community = new OctetString("public");

            // Quick SNMP liveness check — sysDescr is mandatory for any SNMP agent.
            // If this fails, the host has no SNMP agent; skip immediately instead of
            // wasting 3 more timeouts on printer-specific OIDs.
            var sysDescr = await SnmpGetStringAsync(endpoint, community, OidSysDescr);
            if (sysDescr == null)
                return null;

            // Host has SNMP — check printer-specific OIDs (RFC 3805)
            var pageCount = await SnmpGetIntAsync(endpoint, community, OidPrtPageCount);
            var serialNumber = await SnmpGetStringAsync(endpoint, community, OidPrtGeneralSerial);

            // Also check hrDeviceType — printers report OID 1.3.6.1.2.1.25.3.1.5 (printer)
            var deviceType = await SnmpGetStringAsync(endpoint, community, new ObjectIdentifier("1.3.6.1.2.1.25.3.2.1.2.1"));
            var isPrinterByType = deviceType != null && deviceType.Contains("1.3.6.1.2.1.25.3.1.5");

            // If no printer-specific OIDs responded, this isn't a printer
            if (pageCount == null && serialNumber == null && !isPrinterByType)
                return null;

            logger.LogInformation("Printer detected at {Ip} — pageCount={PageCount}, serial={Serial}, deviceType={DeviceType}",
                ip, pageCount, serialNumber, deviceType);

            // It's a printer — gather all the data we can (sysDescr already fetched)
            var deviceDescr = await SnmpGetStringAsync(endpoint, community, OidHrDeviceDescr);
            var (manufacturer, model) = ParseManufacturerModel(sysDescr, deviceDescr);
            var firmwareVersion = ExtractFirmwareVersion(sysDescr);
            var tonerLevels = await GetTonerLevelsAsync(endpoint, community);
            var mac = await GetMacFromArpAsync(ip);
            var status = await GetPrinterStatusAsync(endpoint, community);

            return new NetworkPrinterInfo(
                IpAddress: ip.ToString(),
                MacAddress: mac,
                Manufacturer: manufacturer,
                Model: model,
                SerialNumber: serialNumber,
                FirmwareVersion: firmwareVersion,
                PageCount: pageCount,
                TonerBlackPercent: tonerLevels.GetValueOrDefault("black"),
                TonerCyanPercent: tonerLevels.GetValueOrDefault("cyan"),
                TonerMagentaPercent: tonerLevels.GetValueOrDefault("magenta"),
                TonerYellowPercent: tonerLevels.GetValueOrDefault("yellow"),
                Status: status);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDebug(ex, "SNMP scan failed for {Ip}", ip);
            return null;
        }
    }

    private async Task<string?> SnmpGetStringAsync(IPEndPoint endpoint, OctetString community, ObjectIdentifier oid)
    {
        try
        {
            using var cts = new CancellationTokenSource(SnmpTimeout);
            var task = Messenger.GetAsync(
                VersionCode.V2,
                endpoint,
                community,
                [new Variable(oid)]);
            var result = await task.WaitAsync(cts.Token);

            if (result.Count > 0 && result[0].Data is not NoSuchInstance and not NoSuchObject and not EndOfMibView)
                return result[0].Data.ToString();
        }
        catch { }
        return null;
    }

    private async Task<int?> SnmpGetIntAsync(IPEndPoint endpoint, OctetString community, ObjectIdentifier oid)
    {
        try
        {
            using var cts = new CancellationTokenSource(SnmpTimeout);
            var task = Messenger.GetAsync(
                VersionCode.V2,
                endpoint,
                community,
                [new Variable(oid)]);
            var result = await task.WaitAsync(cts.Token);

            if (result.Count > 0 && result[0].Data is Integer32 intVal)
                return intVal.ToInt32();
            if (result.Count > 0 && result[0].Data is Counter32 cntVal)
                return (int)cntVal.ToUInt32();
        }
        catch { }
        return null;
    }

    private async Task<Dictionary<string, int?>> GetTonerLevelsAsync(IPEndPoint endpoint, OctetString community)
    {
        var levels = new Dictionary<string, int?>();

        // Query up to 8 supply slots
        for (var i = 1; i <= 8; i++)
        {
            var descOid = new ObjectIdentifier($"{OidSupplyDescBase}.{i}");
            var maxOid = new ObjectIdentifier($"{OidSupplyMaxBase}.{i}");
            var levelOid = new ObjectIdentifier($"{OidSupplyLevelBase}.{i}");

            var desc = await SnmpGetStringAsync(endpoint, community, descOid);
            if (desc == null) break; // No more supplies

            var max = await SnmpGetIntAsync(endpoint, community, maxOid);
            var level = await SnmpGetIntAsync(endpoint, community, levelOid);

            if (max is > 0 && level.HasValue)
            {
                var percent = (int)Math.Round(level.Value * 100.0 / max.Value);
                percent = Math.Clamp(percent, 0, 100);

                var color = ClassifyTonerColor(desc);
                if (color != null)
                    levels[color] = percent;
            }
            else if (level is -3) // -3 means "some remaining" per RFC 3805
            {
                var color = ClassifyTonerColor(desc);
                if (color != null)
                    levels[color] = null; // Unknown level, but present
            }
        }

        return levels;
    }

    private static string? ClassifyTonerColor(string description)
    {
        var lower = description.ToLowerInvariant();
        if (lower.Contains("black") || lower.Contains("bk")) return "black";
        if (lower.Contains("cyan") || lower.Contains("c ")) return "cyan";
        if (lower.Contains("magenta") || lower.Contains("m ")) return "magenta";
        if (lower.Contains("yellow") || lower.Contains("y ")) return "yellow";
        return null;
    }

    private async Task<string> GetPrinterStatusAsync(IPEndPoint endpoint, OctetString community)
    {
        // hrPrinterStatus OID: 1.3.6.1.2.1.25.3.5.1.1.1
        // Values: 1=other, 2=unknown, 3=idle, 4=printing, 5=warmup
        var status = await SnmpGetIntAsync(endpoint, community,
            new ObjectIdentifier("1.3.6.1.2.1.25.3.5.1.1.1"));

        // hrPrinterDetectedErrorState: 1.3.6.1.2.1.25.3.5.1.2.1
        var errorState = await SnmpGetStringAsync(endpoint, community,
            new ObjectIdentifier("1.3.6.1.2.1.25.3.5.1.2.1"));

        return status switch
        {
            3 => "Idle",
            4 => "Printing",
            5 => "Warming Up",
            _ => errorState != null ? "Error" : "Online"
        };
    }

    private static (string? Manufacturer, string? Model) ParseManufacturerModel(string? sysDescr, string? deviceDescr)
    {
        // Common patterns: "HP LaserJet Pro M404dn", "Brother HL-L2350DW", "RICOH MP C3004"
        string? manufacturer = null;
        var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HP"] = "HP", ["Hewlett-Packard"] = "HP", ["Hewlett Packard"] = "HP",
            ["Brother"] = "Brother", ["Canon"] = "Canon", ["Epson"] = "Epson",
            ["Ricoh"] = "Ricoh", ["Xerox"] = "Xerox", ["Lexmark"] = "Lexmark",
            ["Kyocera"] = "Kyocera", ["Samsung"] = "Samsung", ["Konica"] = "Konica Minolta",
            ["Sharp"] = "Sharp", ["OKI"] = "OKI", ["Dell"] = "Dell",
        };

        // Check both strings for manufacturer (sysDescr often has it even when deviceDescr doesn't)
        foreach (var source in new[] { deviceDescr, sysDescr })
        {
            if (string.IsNullOrWhiteSpace(source)) continue;
            foreach (var (prefix, brand) in known)
            {
                if (source.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    manufacturer = brand;
                    break;
                }
            }
            if (manufacturer != null) break;
        }

        // Use deviceDescr for model name (more descriptive), fall back to sysDescr
        var modelSource = deviceDescr ?? sysDescr;
        var model = modelSource?.Split(['\n', '\r', ';'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim();
        if (model is { Length: > 100 }) model = model[..100];

        return (manufacturer, model);
    }

    private static string? ExtractFirmwareVersion(string? sysDescr)
    {
        if (sysDescr == null) return null;
        // Look for patterns like "FW:2.73.1", "firmware 1.2.3", "V4.5.6"
        var patterns = new[] { "FW:", "firmware ", "Firmware:", "V" };
        foreach (var pattern in patterns)
        {
            var idx = sysDescr.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var start = idx + pattern.Length;
            var end = start;
            while (end < sysDescr.Length && (char.IsDigit(sysDescr[end]) || sysDescr[end] == '.'))
                end++;
            if (end > start) return sysDescr[start..end];
        }
        return null;
    }

    private static async Task<string?> GetMacFromArpAsync(IPAddress ip)
    {
        try
        {
            // Ping first to populate ARP cache
            using var ping = new Ping();
            await ping.SendPingAsync(ip, 500);

            // Try to read ARP on different platforms
            if (OperatingSystem.IsWindows())
                return ParseArpOutput("arp", $"-a {ip}", ip.ToString());
            if (OperatingSystem.IsMacOS())
                return ParseArpOutput("arp", $"-n {ip}", ip.ToString());
            if (OperatingSystem.IsLinux())
                return ParseArpOutput("arp", $"-n {ip}", ip.ToString());
        }
        catch { }
        return null;
    }

    private static string? ParseArpOutput(string command, string args, string ip)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(command, args)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return null;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);

            // Find MAC address pattern (xx:xx:xx:xx:xx:xx or xx-xx-xx-xx-xx-xx)
            foreach (var line in output.Split('\n'))
            {
                if (!line.Contains(ip)) continue;
                var match = System.Text.RegularExpressions.Regex.Match(line,
                    @"([\da-fA-F]{1,2}[:\-][\da-fA-F]{1,2}[:\-][\da-fA-F]{1,2}[:\-][\da-fA-F]{1,2}[:\-][\da-fA-F]{1,2}[:\-][\da-fA-F]{1,2})");
                if (match.Success) return match.Groups[1].Value.ToUpperInvariant().Replace('-', ':');
            }
        }
        catch { }
        return null;
    }

    private static List<(IPAddress Address, IPAddress Mask)> GetLocalSubnets()
    {
        var subnets = new List<(IPAddress, IPAddress)>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

            foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (IPAddress.IsLoopback(unicast.Address)) continue;
                if (unicast.IPv4Mask == null) continue;

                subnets.Add((unicast.Address, unicast.IPv4Mask));
            }
        }

        return subnets;
    }

    private static List<IPAddress> GetSubnetHosts(IPAddress address, IPAddress mask)
    {
        var addrBytes = address.GetAddressBytes();
        var maskBytes = mask.GetAddressBytes();

        var networkBytes = new byte[4];
        var broadcastBytes = new byte[4];
        for (var i = 0; i < 4; i++)
        {
            networkBytes[i] = (byte)(addrBytes[i] & maskBytes[i]);
            broadcastBytes[i] = (byte)(addrBytes[i] | ~maskBytes[i]);
        }

        var networkInt = BitConverter.ToUInt32(networkBytes.Reverse().ToArray(), 0);
        var broadcastInt = BitConverter.ToUInt32(broadcastBytes.Reverse().ToArray(), 0);
        var hostCount = broadcastInt - networkInt - 1;

        // Limit to /24 at most
        if (hostCount > 254) hostCount = 254;

        var hosts = new List<IPAddress>();
        for (uint i = 1; i <= hostCount; i++)
        {
            var hostInt = networkInt + i;
            var hostBytes = BitConverter.GetBytes(hostInt).Reverse().ToArray();
            var hostIp = new IPAddress(hostBytes);
            if (hostIp.Equals(address)) continue;
            hosts.Add(hostIp);
        }

        return hosts;
    }
}
