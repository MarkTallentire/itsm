using Itsm.Common;
using Itsm.Common.Models;

namespace Itsm.Agent;

public class LinuxPeripheralGatherer(ICommandRunner commandRunner) : IPeripheralGatherer
{
    public List<MonitorInfo> GetMonitors()
    {
        try
        {
            var output = commandRunner.Run("xrandr", "--verbose");
            var monitors = new List<MonitorInfo>();
            string? currentName = null;
            int? width = null, height = null;

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();

                // Connected output line: "HDMI-1 connected 1920x1080+0+0 ..."
                if (trimmed.Contains(" connected", StringComparison.Ordinal))
                {
                    // Save previous monitor
                    if (currentName != null)
                        monitors.Add(new MonitorInfo("Unknown", currentName, null, null, width, height, null));

                    currentName = trimmed.Split(' ')[0];
                    width = null;
                    height = null;

                    // Try to parse resolution from the connected line
                    var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        if (part.Contains('x') && part.Contains('+'))
                        {
                            var res = part.Split('+')[0].Split('x');
                            if (res.Length == 2 && int.TryParse(res[0], out var w) && int.TryParse(res[1], out var h))
                            {
                                width = w;
                                height = h;
                            }
                            break;
                        }
                    }
                }
                else if (currentName != null && trimmed.Contains("Manufacturer:", StringComparison.OrdinalIgnoreCase))
                {
                    // Some xrandr verbose output includes EDID-decoded manufacturer
                }
            }

            // Don't forget the last monitor
            if (currentName != null)
                monitors.Add(new MonitorInfo("Unknown", currentName, null, null, width, height, null));

            return monitors;
        }
        catch
        {
            return [];
        }
    }

    public List<UsbDeviceInfo> GetUsbDevices()
    {
        try
        {
            var output = commandRunner.Run("lsusb", "");
            var devices = new List<UsbDeviceInfo>();

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // Format: "Bus 001 Device 002: ID 1234:5678 Manufacturer Product Name"
                var idIdx = trimmed.IndexOf("ID ", StringComparison.Ordinal);
                if (idIdx < 0) continue;

                var afterId = trimmed[(idIdx + 3)..];
                var colonIdx = afterId.IndexOf(':');
                if (colonIdx < 0 || colonIdx + 1 >= afterId.Length) continue;

                var vendorId = afterId[..colonIdx].Trim();

                // Product ID is the next 4 characters after the colon
                var rest = afterId[(colonIdx + 1)..].Trim();
                var spaceIdx = rest.IndexOf(' ');
                string productId;
                string name;

                if (spaceIdx > 0)
                {
                    productId = rest[..spaceIdx].Trim();
                    name = rest[(spaceIdx + 1)..].Trim();
                }
                else
                {
                    productId = rest.Trim();
                    name = "Unknown USB Device";
                }

                // Skip root hubs
                if (name.Contains("root hub", StringComparison.OrdinalIgnoreCase))
                    continue;

                devices.Add(new UsbDeviceInfo(vendorId, productId, name, null, null));
            }

            return devices;
        }
        catch
        {
            return [];
        }
    }
}
