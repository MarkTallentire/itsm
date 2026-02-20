using System.Net.Http.Json;
using Itsm.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Itsm.Api.Tests;

public class PeripheralEndpointTests : IClassFixture<ItsmApiFactory>, IDisposable
{
    private readonly ItsmApiFactory _factory;
    private readonly HttpClient _client;

    public PeripheralEndpointTests(ItsmApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task PostPeripherals_CreatesMonitorAssetsAndModels()
    {
        var report = TestFixtures.CreateTestPeripheralReport(
            uuid: "uuid-mon-1",
            monitors: [new MonitorInfo("Dell", "U2723QE", "MON-SN-001", 2023, 3840, 2160, 27.0)],
            usbDevices: [],
            printers: []);

        var response = await _client.PostAsJsonAsync("/inventory/peripherals", report);
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        var model = await db.MonitorModels.FirstOrDefaultAsync(m => m.Manufacturer == "Dell" && m.ModelName == "U2723QE");
        Assert.NotNull(model);
        Assert.Equal(3840, model.WidthPixels);
        Assert.Equal(2160, model.HeightPixels);
        Assert.Equal(27.0, model.DiagonalInches);

        var monitor = await db.Monitors.Include(m => m.Asset).FirstOrDefaultAsync(m => m.MonitorModelId == model.Id);
        Assert.NotNull(monitor);
        Assert.Equal(2023, monitor.ManufactureYear);
        Assert.Equal("Monitor", monitor.Asset.Type);
        Assert.Equal("Dell U2723QE", monitor.Asset.Name);
        Assert.Equal("MON-SN-001", monitor.Asset.SerialNumber);
    }

    [Fact]
    public async Task PostPeripherals_DeduplicatesMonitorModels()
    {
        var monitor = new MonitorInfo("LG", "27UK850", "MON-DEDUP-1", 2022, 3840, 2160, 27.0);

        var report1 = TestFixtures.CreateTestPeripheralReport(
            uuid: "uuid-mon-dedup-1",
            monitors: [monitor],
            usbDevices: [],
            printers: []);

        var monitor2 = new MonitorInfo("LG", "27UK850", "MON-DEDUP-2", 2023, 3840, 2160, 27.0);
        var report2 = TestFixtures.CreateTestPeripheralReport(
            uuid: "uuid-mon-dedup-2",
            monitors: [monitor2],
            usbDevices: [],
            printers: []);

        await _client.PostAsJsonAsync("/inventory/peripherals", report1);
        await _client.PostAsJsonAsync("/inventory/peripherals", report2);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        var models = await db.MonitorModels.Where(m => m.Manufacturer == "LG" && m.ModelName == "27UK850").ToListAsync();
        Assert.Single(models);

        // But there should be two monitor instances (different serials)
        var monitors = await db.Monitors.Where(m => m.MonitorModelId == models[0].Id).ToListAsync();
        Assert.Equal(2, monitors.Count);
    }

    [Fact]
    public async Task PostPeripherals_PreservesUserEditedFields_OnRediscovery()
    {
        var report1 = TestFixtures.CreateTestPeripheralReport(
            uuid: "uuid-preserve-1",
            monitors: [new MonitorInfo("Samsung", "Odyssey G7", "MON-PRESERVE-1", 2023, 2560, 1440, 32.0)],
            usbDevices: [],
            printers: []);

        await _client.PostAsJsonAsync("/inventory/peripherals", report1);

        // Manually update asset fields
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();
            var asset = await db.Assets.FirstAsync(a => a.SerialNumber == "MON-PRESERVE-1");
            asset.AssignedUser = "custom-user";
            asset.Location = "Room 42";
            asset.Status = "InStorage";
            asset.Notes = "User notes";
            asset.Cost = 599.99m;
            await db.SaveChangesAsync();
        }

        // Re-discover same monitor
        var report2 = TestFixtures.CreateTestPeripheralReport(
            uuid: "uuid-preserve-2",
            monitors: [new MonitorInfo("Samsung", "Odyssey G7", "MON-PRESERVE-1", 2023, 2560, 1440, 32.0)],
            usbDevices: [],
            printers: []);

        await _client.PostAsJsonAsync("/inventory/peripherals", report2);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();
            var asset = await db.Assets.FirstAsync(a => a.SerialNumber == "MON-PRESERVE-1");

            // User-edited fields should be preserved
            Assert.Equal("custom-user", asset.AssignedUser);
            Assert.Equal("Room 42", asset.Location);
            Assert.Equal("InStorage", asset.Status);
            Assert.Equal("User notes", asset.Notes);
            Assert.Equal(599.99m, asset.Cost);

            // Agent-sourced fields should be updated
            Assert.Equal("uuid-preserve-2", asset.DiscoveredByAgent);
        }
    }

    [Fact]
    public async Task PostPeripherals_UsbWithSerial_CreatesAsset()
    {
        var report = TestFixtures.CreateTestPeripheralReport(
            uuid: "uuid-usb-1",
            monitors: [],
            usbDevices: [new UsbDeviceInfo("046d", "c52b", "Logitech Receiver", "Logitech", "USB-SN-001")],
            printers: []);

        var response = await _client.PostAsJsonAsync("/inventory/peripherals", report);
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        var product = await db.UsbProducts.FirstOrDefaultAsync(p => p.VendorId == "046d" && p.ProductId == "c52b");
        Assert.NotNull(product);
        Assert.Equal("Logitech Receiver", product.Name);

        var peripheral = await db.UsbPeripherals.Include(u => u.Asset).FirstOrDefaultAsync(u => u.UsbProductId == product.Id);
        Assert.NotNull(peripheral);
        Assert.Equal("UsbPeripheral", peripheral.Asset.Type);
        Assert.Equal("USB-SN-001", peripheral.Asset.SerialNumber);
    }

    [Fact]
    public async Task PostPeripherals_UsbWithoutSerial_SkipsAssetCreation()
    {
        var report = TestFixtures.CreateTestPeripheralReport(
            uuid: "uuid-usb-noserial",
            monitors: [],
            usbDevices: [
                new UsbDeviceInfo("1234", "5678", "No Serial Device", "Generic", null),
                new UsbDeviceInfo("1234", "9999", "Empty Serial Device", "Generic", "")
            ],
            printers: []);

        await _client.PostAsJsonAsync("/inventory/peripherals", report);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        // No assets should be created for these USB devices
        var assets = await db.Assets.Where(a => a.DiscoveredByAgent == "uuid-usb-noserial" && a.Type == "UsbPeripheral").ToListAsync();
        Assert.Empty(assets);
    }

    [Fact]
    public async Task PostPeripherals_Printers_CreatesModelsAndAssets()
    {
        var report = TestFixtures.CreateTestPeripheralReport(
            uuid: "uuid-printer-1",
            monitors: [],
            usbDevices: [],
            printers: [new NetworkPrinterInfo("10.0.0.50", "AA:BB:CC:DD:EE:01", "HP", "LaserJet Pro", null, null, null, null, null, null, null, null)]);

        var response = await _client.PostAsJsonAsync("/inventory/peripherals", report);
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        var model = await db.PrinterModels.FirstOrDefaultAsync(p => p.Manufacturer == "HP" && p.Model == "LaserJet Pro");
        Assert.NotNull(model);

        var printer = await db.NetworkPrinters.Include(p => p.Asset).FirstOrDefaultAsync(p => p.PrinterModelId == model.Id);
        Assert.NotNull(printer);
        Assert.Equal("10.0.0.50", printer.IpAddress);
        Assert.Equal("AA:BB:CC:DD:EE:01", printer.MacAddress);
        Assert.Equal("NetworkPrinter", printer.Asset.Type);
        Assert.Equal("HP LaserJet Pro", printer.Asset.Name);
    }

    [Fact]
    public async Task PostPeripherals_Printers_DeduplicatesByIpAddress()
    {
        var report1 = TestFixtures.CreateTestPeripheralReport(
            uuid: "uuid-printer-dedup-1",
            monitors: [],
            usbDevices: [],
            printers: [new NetworkPrinterInfo("10.0.0.99", "AA:BB:CC:00:00:01", "HP", "OldModel", null, null, null, null, null, null, null, null)]);

        var report2 = TestFixtures.CreateTestPeripheralReport(
            uuid: "uuid-printer-dedup-2",
            monitors: [],
            usbDevices: [],
            printers: [new NetworkPrinterInfo("10.0.0.99", "AA:BB:CC:00:00:02", "HP", "NewModel", null, null, null, null, null, null, null, null)]);

        await _client.PostAsJsonAsync("/inventory/peripherals", report1);
        await _client.PostAsJsonAsync("/inventory/peripherals", report2);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        var printers = await db.NetworkPrinters.Where(p => p.IpAddress == "10.0.0.99").ToListAsync();
        Assert.Single(printers);

        // MAC should be updated
        Assert.Equal("AA:BB:CC:00:00:02", printers[0].MacAddress);
    }
}
