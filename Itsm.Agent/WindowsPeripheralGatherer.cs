using Itsm.Common;
using Itsm.Common.Models;

namespace Itsm.Agent;

public class WindowsPeripheralGatherer(ICommandRunner commandRunner) : IPeripheralGatherer
{
    public List<MonitorInfo> GetMonitors()
    {
        try
        {
            var output = commandRunner.Run("wmic", "desktopmonitor get Name,MonitorManufacturer,ScreenHeight,ScreenWidth /format:list");
            var monitors = new List<MonitorInfo>();

            var blocks = output.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var block in blocks)
            {
                var name = WindowsHardwareGatherer.ParseWmicValue(block, "Name");
                if (name == "Unknown") continue;

                var manufacturer = WindowsHardwareGatherer.ParseWmicValue(block, "MonitorManufacturer");
                var heightStr = WindowsHardwareGatherer.ParseWmicValue(block, "ScreenHeight");
                var widthStr = WindowsHardwareGatherer.ParseWmicValue(block, "ScreenWidth");

                int? width = int.TryParse(widthStr, out var w) ? w : null;
                int? height = int.TryParse(heightStr, out var h) ? h : null;

                monitors.Add(new MonitorInfo(
                    manufacturer != "Unknown" ? manufacturer : "Unknown",
                    name,
                    null, null,
                    width, height, null));
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
            var output = commandRunner.Run("powershell", "-Command \"Get-CimInstance Win32_PnPEntity | Where-Object { $_.PNPDeviceID -like 'USB*' } | Select-Object Name,Manufacturer,PNPDeviceID | Format-List\"");
            var devices = new List<UsbDeviceInfo>();

            var blocks = output.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var block in blocks)
            {
                var name = WindowsHardwareGatherer.ParsePowerShellValue(block, "Name");
                if (name == "Unknown" || string.IsNullOrWhiteSpace(name)) continue;

                var manufacturer = WindowsHardwareGatherer.ParsePowerShellValue(block, "Manufacturer");
                var pnpId = WindowsHardwareGatherer.ParsePowerShellValue(block, "PNPDeviceID");

                // Parse VID and PID from PNPDeviceID format: USB\VID_xxxx&PID_xxxx\serial
                var vendorId = "";
                var productId = "";
                string? serial = null;

                if (pnpId != "Unknown")
                {
                    var vidIdx = pnpId.IndexOf("VID_", StringComparison.OrdinalIgnoreCase);
                    if (vidIdx >= 0 && vidIdx + 8 <= pnpId.Length)
                        vendorId = pnpId.Substring(vidIdx + 4, 4);

                    var pidIdx = pnpId.IndexOf("PID_", StringComparison.OrdinalIgnoreCase);
                    if (pidIdx >= 0 && pidIdx + 8 <= pnpId.Length)
                        productId = pnpId.Substring(pidIdx + 4, 4);

                    // Serial is the last segment after the second backslash
                    var segments = pnpId.Split('\\');
                    if (segments.Length >= 3 && !segments[2].Contains('&'))
                        serial = segments[2];
                }

                devices.Add(new UsbDeviceInfo(
                    vendorId, productId, name,
                    manufacturer != "Unknown" ? manufacturer : null,
                    serial));
            }

            return devices;
        }
        catch
        {
            return [];
        }
    }
}
