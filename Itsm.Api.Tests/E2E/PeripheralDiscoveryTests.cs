using System.Net.Http.Json;
using System.Text.Json;
using Itsm.Common.Models;
using Xunit;

namespace Itsm.Api.Tests.E2E;

public class PeripheralDiscoveryTests : IClassFixture<ItsmApiFactory>, IDisposable
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public PeripheralDiscoveryTests(ItsmApiFactory factory) => _client = factory.CreateClient();
    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task MonitorDiscovery_CreatesModelAndAsset()
    {
        var report = TestFixtures.CreateTestPeripheralReport(
            uuid: "periph-mon-uuid",
            computerName: "periph-mon-pc",
            monitors: [new MonitorInfo("Dell", "P2722H-e2e", "MON-E2E-001", 2023, 1920, 1080, 27.0)],
            usbDevices: [],
            printers: []);

        var response = await _client.PostAsJsonAsync("/inventory/peripherals", report);
        response.EnsureSuccessStatusCode();

        var assets = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Monitor", JsonOpts);
        var monitorAsset = assets.EnumerateArray()
            .FirstOrDefault(a => a.GetProperty("serialNumber").GetString() == "MON-E2E-001");
        Assert.NotEqual(default, monitorAsset);
        Assert.Equal("Dell P2722H-e2e", monitorAsset.GetProperty("name").GetString());
        Assert.Equal("InUse", monitorAsset.GetProperty("status").GetString());
    }

    [Fact]
    public async Task UsbDiscovery_OnlyCreatesAssetsWithSerial()
    {
        var report = TestFixtures.CreateTestPeripheralReport(
            uuid: "periph-usb-filter-uuid",
            computerName: "periph-usb-pc",
            monitors: [],
            usbDevices: [
                new UsbDeviceInfo("abcd", "1111", "USB Keyboard", "Corsair", "USB-E2E-WITH-SERIAL"),
                new UsbDeviceInfo("abcd", "2222", "USB Mouse", "Corsair", null)
            ],
            printers: []);

        await _client.PostAsJsonAsync("/inventory/peripherals", report);

        var assets = await _client.GetFromJsonAsync<JsonElement>("/assets?type=UsbPeripheral", JsonOpts);
        var usbAssets = assets.EnumerateArray()
            .Where(a => a.GetProperty("name").GetString()!.Contains("USB"))
            .ToList();

        // Only the one with serial should exist
        var withSerial = usbAssets.FirstOrDefault(a =>
            a.GetProperty("serialNumber").GetString() == "USB-E2E-WITH-SERIAL");
        Assert.NotEqual(default, withSerial);

        var withoutSerial = usbAssets.FirstOrDefault(a =>
            a.GetProperty("name").GetString() == "USB Mouse" &&
            (a.GetProperty("serialNumber").ValueKind == JsonValueKind.Null ||
             string.IsNullOrEmpty(a.GetProperty("serialNumber").GetString())));
        Assert.Equal(default, withoutSerial);
    }

    [Fact]
    public async Task PrinterDiscovery_CreatesModelAndAsset()
    {
        var report = TestFixtures.CreateTestPeripheralReport(
            uuid: "periph-printer-uuid",
            computerName: "periph-printer-pc",
            monitors: [],
            usbDevices: [],
            printers: [new NetworkPrinterInfo("10.0.0.77", "AA:BB:CC:00:77:01", "Brother", "HL-L2350DW-e2e", null, null, null, null, null, null, null, null)]);

        var response = await _client.PostAsJsonAsync("/inventory/peripherals", report);
        response.EnsureSuccessStatusCode();

        var assets = await _client.GetFromJsonAsync<JsonElement>("/assets?type=NetworkPrinter", JsonOpts);
        var printerAsset = assets.EnumerateArray()
            .FirstOrDefault(a => a.GetProperty("name").GetString() == "Brother HL-L2350DW-e2e");
        Assert.NotEqual(default, printerAsset);
        Assert.Equal("Agent", printerAsset.GetProperty("source").GetString());
    }

    [Fact]
    public async Task FullPeripheralReport_CreatesAllAssetTypes()
    {
        var report = TestFixtures.CreateTestPeripheralReport(
            uuid: "periph-full-uuid",
            computerName: "periph-full-pc",
            monitors: [
                new MonitorInfo("LG", "27GP950-e2e", "MON-FULL-001", 2023, 3840, 2160, 27.0),
                new MonitorInfo("LG", "27GP950-e2e", "MON-FULL-002", 2022, 3840, 2160, 27.0)
            ],
            usbDevices: [
                new UsbDeviceInfo("1234", "e2e1", "Webcam", "Logitech", "USB-FULL-001"),
                new UsbDeviceInfo("1234", "e2e2", "Headset", "Jabra", null) // no serial - skipped
            ],
            printers: [
                new NetworkPrinterInfo("10.0.0.88", "AA:BB:CC:00:88:01", "Xerox", "WorkCentre-e2e", null, null, null, null, null, null, null, null)
            ]);

        await _client.PostAsJsonAsync("/inventory/peripherals", report);

        // Check monitors
        var monitorAssets = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Monitor", JsonOpts);
        var monitorMatches = monitorAssets.EnumerateArray()
            .Where(a => a.GetProperty("serialNumber").GetString()?.StartsWith("MON-FULL-") == true)
            .ToList();
        Assert.Equal(2, monitorMatches.Count);

        // Check USB - only one with serial
        var usbAssets = await _client.GetFromJsonAsync<JsonElement>("/assets?type=UsbPeripheral", JsonOpts);
        var usbMatch = usbAssets.EnumerateArray()
            .FirstOrDefault(a => a.GetProperty("serialNumber").GetString() == "USB-FULL-001");
        Assert.NotEqual(default, usbMatch);

        // Check printers
        var printerAssets = await _client.GetFromJsonAsync<JsonElement>("/assets?type=NetworkPrinter", JsonOpts);
        var printerMatch = printerAssets.EnumerateArray()
            .FirstOrDefault(a => a.GetProperty("name").GetString() == "Xerox WorkCentre-e2e");
        Assert.NotEqual(default, printerMatch);
    }
}
