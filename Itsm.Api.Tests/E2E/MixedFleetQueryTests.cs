using System.Net.Http.Json;
using System.Text.Json;
using Itsm.Common.Models;

namespace Itsm.Api.Tests.E2E;

public class MixedFleetQueryTests : IClassFixture<ItsmApiFactory>, IDisposable
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public MixedFleetQueryTests(ItsmApiFactory factory) => _client = factory.CreateClient();
    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task MixedFleet_AllAssetTypesQueryable()
    {
        // Set up: 2 computers
        await _client.PostAsJsonAsync("/inventory/computer",
            TestFixtures.CreateTestComputer(name: "fleet-comp-1", uuid: "fleet-comp-uuid-1"));
        await _client.PostAsJsonAsync("/inventory/computer",
            TestFixtures.CreateTestComputer(name: "fleet-comp-2", uuid: "fleet-comp-uuid-2"));

        // 2 monitors + 1 printer via peripherals
        await _client.PostAsJsonAsync("/inventory/peripherals",
            TestFixtures.CreateTestPeripheralReport(
                uuid: "fleet-periph-uuid",
                computerName: "fleet-comp-1",
                monitors: [
                    new MonitorInfo("Dell", "U2723QE-fleet", "MON-FLEET-001", 2023, 3840, 2160, 27.0),
                    new MonitorInfo("Dell", "U2723QE-fleet", "MON-FLEET-002", 2022, 3840, 2160, 27.0)
                ],
                usbDevices: [],
                printers: [
                    new NetworkPrinterInfo("10.0.0.111", "AA:BB:CC:00:FF:01", "HP", "LaserJet-fleet", null, null, null, null, null, null, null, null)
                ]));

        // 1 manual phone
        await _client.PostAsJsonAsync("/assets", new
        {
            Name = "iPhone-fleet-test",
            Type = "Phone",
            Status = "InUse"
        });

        // GET /assets returns all types
        var allAssets = await _client.GetFromJsonAsync<JsonElement>("/assets", JsonOpts);
        var allTypes = allAssets.EnumerateArray()
            .Select(a => a.GetProperty("type").GetString())
            .Distinct()
            .ToList();
        Assert.Contains("Computer", allTypes);
        Assert.Contains("Monitor", allTypes);
        Assert.Contains("NetworkPrinter", allTypes);
        Assert.Contains("Phone", allTypes);

        // Filter by Computer
        var computers = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Computer", JsonOpts);
        foreach (var c in computers.EnumerateArray())
            Assert.Equal("Computer", c.GetProperty("type").GetString());
        Assert.True(computers.GetArrayLength() >= 2);

        // Filter by Monitor
        var monitors = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Monitor", JsonOpts);
        foreach (var m in monitors.EnumerateArray())
            Assert.Equal("Monitor", m.GetProperty("type").GetString());
        Assert.True(monitors.GetArrayLength() >= 2);

        // Search by computer name
        var searchComp = await _client.GetFromJsonAsync<JsonElement>("/assets?search=fleet-comp-1", JsonOpts);
        Assert.True(searchComp.GetArrayLength() >= 1);
        Assert.True(searchComp.EnumerateArray().Any(a =>
            a.GetProperty("name").GetString() == "fleet-comp-1"));

        // Search by phone name
        var searchPhone = await _client.GetFromJsonAsync<JsonElement>("/assets?search=iPhone-fleet", JsonOpts);
        Assert.True(searchPhone.GetArrayLength() >= 1);
        Assert.True(searchPhone.EnumerateArray().Any(a =>
            a.GetProperty("name").GetString() == "iPhone-fleet-test"));
    }
}
