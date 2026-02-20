using System.Net.Http.Json;
using System.Text.Json;
using Itsm.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Itsm.Api.Tests.E2E;

public class DeduplicationTests : IClassFixture<ItsmApiFactory>, IDisposable
{
    private readonly ItsmApiFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public DeduplicationTests(ItsmApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task SameGpuOnTwoComputers_OneModelRow()
    {
        var gpu = new GpuInfo("NVIDIA RTX 4090-dedup", "NVIDIA", 16_000_000_000, "535.100");
        var compA = TestFixtures.CreateTestComputer(
            name: "dedup-gpu-pc-a",
            uuid: "dedup-gpu-uuid-a",
            gpus: [gpu]);

        var gpuDiffVram = new GpuInfo("NVIDIA RTX 4090-dedup", "NVIDIA", 24_000_000_000, "535.200");
        var compB = TestFixtures.CreateTestComputer(
            name: "dedup-gpu-pc-b",
            uuid: "dedup-gpu-uuid-b",
            gpus: [gpuDiffVram]);

        await _client.PostAsJsonAsync("/inventory/computer", compA);
        await _client.PostAsJsonAsync("/inventory/computer", compB);

        // Both computers should have the GPU in their responses
        var respA = await _client.GetFromJsonAsync<JsonElement>("/inventory/computers/dedup-gpu-pc-a", JsonOpts);
        var respB = await _client.GetFromJsonAsync<JsonElement>("/inventory/computers/dedup-gpu-pc-b", JsonOpts);

        var gpuA = respA.GetProperty("data").GetProperty("gpus").EnumerateArray().First();
        var gpuB = respB.GetProperty("data").GetProperty("gpus").EnumerateArray().First();

        Assert.Equal("NVIDIA RTX 4090-dedup", gpuA.GetProperty("name").GetString());
        Assert.Equal("NVIDIA RTX 4090-dedup", gpuB.GetProperty("name").GetString());

        // VRAM should be per-machine
        Assert.Equal(16_000_000_000, gpuA.GetProperty("vramBytes").GetInt64());
        Assert.Equal(24_000_000_000, gpuB.GetProperty("vramBytes").GetInt64());

        // Verify dedup at DB level: only one GpuModel row
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();
        var gpuModels = await db.GpuModels.Where(g => g.Name == "NVIDIA RTX 4090-dedup").ToListAsync();
        Assert.Single(gpuModels);
    }

    [Fact]
    public async Task SameSoftwareOnTwoComputers_OneTitleRow()
    {
        var chromeV120 = new InstalledApp("Google Chrome-dedup", "120.0.0.0", "2024-01-01");
        var chromeV121 = new InstalledApp("Google Chrome-dedup", "121.0.0.0", "2024-02-01");

        var compA = TestFixtures.CreateTestComputer(
            name: "dedup-sw-pc-a",
            uuid: "dedup-sw-uuid-a",
            apps: [chromeV120]);
        var compB = TestFixtures.CreateTestComputer(
            name: "dedup-sw-pc-b",
            uuid: "dedup-sw-uuid-b",
            apps: [chromeV121]);

        await _client.PostAsJsonAsync("/inventory/computer", compA);
        await _client.PostAsJsonAsync("/inventory/computer", compB);

        // Both computers should list Chrome in their software
        var respA = await _client.GetFromJsonAsync<JsonElement>("/inventory/computers/dedup-sw-pc-a", JsonOpts);
        var respB = await _client.GetFromJsonAsync<JsonElement>("/inventory/computers/dedup-sw-pc-b", JsonOpts);

        var appsA = respA.GetProperty("data").GetProperty("installedApps").EnumerateArray()
            .Where(a => a.GetProperty("name").GetString() == "Google Chrome-dedup").ToList();
        var appsB = respB.GetProperty("data").GetProperty("installedApps").EnumerateArray()
            .Where(a => a.GetProperty("name").GetString() == "Google Chrome-dedup").ToList();

        Assert.Single(appsA);
        Assert.Single(appsB);
        Assert.Equal("120.0.0.0", appsA[0].GetProperty("version").GetString());
        Assert.Equal("121.0.0.0", appsB[0].GetProperty("version").GetString());

        // Verify dedup at DB level: only one SoftwareTitle row
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();
        var titles = await db.SoftwareTitles.Where(s => s.Name == "Google Chrome-dedup").ToListAsync();
        Assert.Single(titles);
    }

    [Fact]
    public async Task SameMonitorModel_OneDictionaryRow_TwoAssets()
    {
        var report1 = TestFixtures.CreateTestPeripheralReport(
            uuid: "dedup-mon-uuid-1",
            computerName: "dedup-mon-pc-1",
            monitors: [new MonitorInfo("Dell", "U2723QE-dedup", "MON-DEDUP-E2E-001", 2023, 3840, 2160, 27.0)],
            usbDevices: [],
            printers: []);

        var report2 = TestFixtures.CreateTestPeripheralReport(
            uuid: "dedup-mon-uuid-2",
            computerName: "dedup-mon-pc-2",
            monitors: [new MonitorInfo("Dell", "U2723QE-dedup", "MON-DEDUP-E2E-002", 2022, 3840, 2160, 27.0)],
            usbDevices: [],
            printers: []);

        await _client.PostAsJsonAsync("/inventory/peripherals", report1);
        await _client.PostAsJsonAsync("/inventory/peripherals", report2);

        // Both monitors should create separate assets (different serials)
        var assets = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Monitor", JsonOpts);
        var monitorAssets = assets.EnumerateArray()
            .Where(a => a.GetProperty("name").GetString() == "Dell U2723QE-dedup")
            .ToList();
        Assert.Equal(2, monitorAssets.Count);

        // Verify dedup at DB level: one MonitorModel row
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();
        var models = await db.MonitorModels
            .Where(m => m.Manufacturer == "Dell" && m.ModelName == "U2723QE-dedup")
            .ToListAsync();
        Assert.Single(models);
    }
}
