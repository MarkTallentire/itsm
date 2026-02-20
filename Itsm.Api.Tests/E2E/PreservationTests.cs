using System.Net.Http.Json;
using System.Text.Json;
using Itsm.Common.Models;

namespace Itsm.Api.Tests.E2E;

public class PreservationTests : IClassFixture<ItsmApiFactory>, IDisposable
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public PreservationTests(ItsmApiFactory factory) => _client = factory.CreateClient();
    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task RediscoveredMonitor_PreservesUserEdits()
    {
        // Initial discovery
        var report1 = TestFixtures.CreateTestPeripheralReport(
            uuid: "preserve-mon-uuid-1",
            computerName: "preserve-mon-pc",
            monitors: [new MonitorInfo("BenQ", "PD2700U-preserve", "MON-PRESERVE-E2E", 2023, 3840, 2160, 27.0)],
            usbDevices: [],
            printers: []);

        await _client.PostAsJsonAsync("/inventory/peripherals", report1);

        // Find the monitor asset
        var assets = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Monitor", JsonOpts);
        var monitorAsset = assets.EnumerateArray()
            .First(a => a.GetProperty("serialNumber").GetString() == "MON-PRESERVE-E2E");
        var assetId = monitorAsset.GetProperty("id").GetString()!;

        // User edits the asset
        await _client.PutAsJsonAsync($"/assets/{assetId}", new
        {
            Name = "BenQ PD2700U-preserve",
            Type = "Monitor",
            Status = "InUse",
            AssignedUser = "Jane",
            Location = "Building A",
            Cost = 500.00m,
            Notes = "Desk 42"
        });

        // Re-discover the same monitor from a different agent
        var report2 = TestFixtures.CreateTestPeripheralReport(
            uuid: "preserve-mon-uuid-2",
            computerName: "preserve-mon-pc-2",
            monitors: [new MonitorInfo("BenQ", "PD2700U-preserve", "MON-PRESERVE-E2E", 2023, 3840, 2160, 27.0)],
            usbDevices: [],
            printers: []);

        await _client.PostAsJsonAsync("/inventory/peripherals", report2);

        // Verify user edits are preserved
        var getResponse = await _client.GetAsync($"/assets/{assetId}");
        getResponse.EnsureSuccessStatusCode();
        var preserved = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        Assert.Equal("Jane", preserved.GetProperty("assignedUser").GetString());
        Assert.Equal("Building A", preserved.GetProperty("location").GetString());
        Assert.Equal(500.00m, preserved.GetProperty("cost").GetDecimal());
        Assert.Equal("Desk 42", preserved.GetProperty("notes").GetString());

        // Agent-sourced field should be updated
        Assert.Equal("preserve-mon-uuid-2", preserved.GetProperty("discoveredByAgent").GetString());
    }

    [Fact]
    public async Task RediscoveredComputer_PreservesAssetMetadata()
    {
        // Initial report
        var comp1 = TestFixtures.CreateTestComputer(
            name: "preserve-comp-pc",
            uuid: "preserve-comp-uuid",
            serialNumber: "PRESERVE-COMP-SN");

        await _client.PostAsJsonAsync("/inventory/computer", comp1);

        // Find the asset
        var assets = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Computer&search=preserve-comp-pc", JsonOpts);
        var compAsset = assets.EnumerateArray().First();
        var assetId = compAsset.GetProperty("id").GetString()!;

        // User edits the asset
        await _client.PutAsJsonAsync($"/assets/{assetId}", new
        {
            Name = "preserve-comp-pc",
            Type = "Computer",
            Status = "InUse",
            Location = "Server Room",
            AssignedUser = "Bob",
            Notes = "Primary workstation"
        });

        // Re-report the same computer
        var comp2 = TestFixtures.CreateTestComputer(
            name: "preserve-comp-pc",
            uuid: "preserve-comp-uuid",
            serialNumber: "PRESERVE-COMP-SN",
            disks: [new DiskInfo("New SSD", "APFS", 2_000_000_000_000, 1_500_000_000_000)]);

        await _client.PostAsJsonAsync("/inventory/computer", comp2);

        // Verify the asset still has user-edited fields
        var getResponse = await _client.GetAsync($"/assets/{assetId}");
        var result = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        // The computer endpoint updates SerialNumber and DiscoveredByAgent on re-report,
        // but does NOT overwrite Location, AssignedUser, Notes, etc.
        // (These fields are only changed via PUT /assets)
        Assert.Equal("Server Room", result.GetProperty("location").GetString());
        Assert.Equal("Bob", result.GetProperty("assignedUser").GetString());
        Assert.Equal("Primary workstation", result.GetProperty("notes").GetString());
    }
}
