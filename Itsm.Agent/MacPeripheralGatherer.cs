using System.Text.Json;
using Itsm.Common;
using Itsm.Common.Models;

namespace Itsm.Agent;

public class MacPeripheralGatherer(ICommandRunner commandRunner) : IPeripheralGatherer
{
    public List<MonitorInfo> GetMonitors()
    {
        try
        {
            var json = commandRunner.Run("system_profiler", "SPDisplaysDataType -json");
            using var doc = JsonDocument.Parse(json);
            var monitors = new List<MonitorInfo>();

            if (!doc.RootElement.TryGetProperty("SPDisplaysDataType", out var gpuArray))
                return monitors;

            foreach (var gpu in gpuArray.EnumerateArray())
            {
                if (!gpu.TryGetProperty("spdisplays_ndrvs", out var displays))
                    continue;

                foreach (var display in displays.EnumerateArray())
                {
                    // Skip built-in/internal displays — they're part of the laptop, not a separate asset
                    if (display.TryGetProperty("spdisplays_connection_type", out var connType))
                    {
                        var conn = connType.GetString() ?? "";
                        if (conn.Contains("internal", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                    if (display.TryGetProperty("spdisplays_display_type", out var dispType))
                    {
                        var dt = dispType.GetString() ?? "";
                        if (dt.Contains("built-in", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    var name = display.TryGetProperty("_name", out var n) ? n.GetString() ?? "Unknown" : "Unknown";
                    var vendor = display.TryGetProperty("_spdisplays_display-vendor-id", out var v)
                        ? v.GetString() ?? "Unknown"
                        : "Unknown";

                    // Try to get a more readable vendor name from the GPU entry
                    if (vendor == "Unknown" && gpu.TryGetProperty("sppci_vendor", out var gpuVendor))
                        vendor = gpuVendor.GetString() ?? "Unknown";

                    string? serial = display.TryGetProperty("_spdisplays_display-serial-number", out var s)
                        ? s.GetString()
                        : null;

                    int? year = null;
                    if (display.TryGetProperty("_spdisplays_display-year", out var y))
                    {
                        if (y.ValueKind == JsonValueKind.Number)
                            year = y.GetInt32();
                        else if (y.ValueKind == JsonValueKind.String && int.TryParse(y.GetString(), out var yr))
                            year = yr;
                    }

                    int? width = null, height = null;
                    if (display.TryGetProperty("_spdisplays_pixels", out var pixels))
                    {
                        // Format: "3456 x 2234" or similar
                        var pixStr = pixels.GetString();
                        if (pixStr != null)
                        {
                            var parts = pixStr.Split('x', StringSplitOptions.TrimEntries);
                            if (parts.Length == 2)
                            {
                                if (int.TryParse(parts[0], out var w)) width = w;
                                if (int.TryParse(parts[1], out var h)) height = h;
                            }
                        }
                    }
                    else if (display.TryGetProperty("spdisplays_resolution", out var res))
                    {
                        var resStr = res.GetString();
                        if (resStr != null)
                        {
                            // Format: "3456 x 2234" or "3456 x 2234 Retina"
                            var cleaned = resStr.Split(' ');
                            if (cleaned.Length >= 3 && cleaned[1] == "x")
                            {
                                if (int.TryParse(cleaned[0], out var w)) width = w;
                                if (int.TryParse(cleaned[2], out var h)) height = h;
                            }
                        }
                    }

                    monitors.Add(new MonitorInfo(vendor, name, serial, year, width, height, null));
                }
            }

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
            var json = commandRunner.Run("system_profiler", "SPUSBDataType -json");
            using var doc = JsonDocument.Parse(json);
            var devices = new List<UsbDeviceInfo>();

            if (!doc.RootElement.TryGetProperty("SPUSBDataType", out var usbArray))
                return devices;

            foreach (var controller in usbArray.EnumerateArray())
            {
                if (controller.TryGetProperty("_items", out var items))
                    CollectUsbDevices(items, devices);
            }

            return devices;
        }
        catch
        {
            return [];
        }
    }

    private static void CollectUsbDevices(JsonElement items, List<UsbDeviceInfo> devices)
    {
        foreach (var item in items.EnumerateArray())
        {
            var name = item.TryGetProperty("_name", out var n) ? n.GetString() ?? "" : "";

            // Skip built-in and hub devices
            if (name.Contains("Built-in", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Hub", StringComparison.OrdinalIgnoreCase))
            {
                // Still recurse into children — hubs can have real devices attached
                if (item.TryGetProperty("_items", out var children))
                    CollectUsbDevices(children, devices);
                continue;
            }

            var vendorId = item.TryGetProperty("vendor_id", out var vid) ? vid.GetString() ?? "" : "";
            var productId = item.TryGetProperty("product_id", out var pid) ? pid.GetString() ?? "" : "";
            var manufacturer = item.TryGetProperty("manufacturer", out var mfr) ? mfr.GetString() : null;
            var serial = item.TryGetProperty("serial_num", out var sn) ? sn.GetString() : null;

            if (!string.IsNullOrWhiteSpace(name))
                devices.Add(new UsbDeviceInfo(vendorId, productId, name, manufacturer, serial));

            // Recurse into child items
            if (item.TryGetProperty("_items", out var subItems))
                CollectUsbDevices(subItems, devices);
        }
    }
}
