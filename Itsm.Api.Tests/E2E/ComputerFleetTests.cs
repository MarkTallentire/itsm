using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Itsm.Common.Models;
using Xunit;

namespace Itsm.Api.Tests.E2E;

public class ComputerFleetTests : IClassFixture<ItsmApiFactory>, IDisposable
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ComputerFleetTests(ItsmApiFactory factory) => _client = factory.CreateClient();
    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task AgentReportsComputer_AssetAutoCreated()
    {
        var computer = TestFixtures.CreateTestComputer(
            name: "fleet-auto-pc",
            uuid: "fleet-auto-uuid",
            serialNumber: "FLEET-SN-001");

        var postResponse = await _client.PostAsJsonAsync("/inventory/computer", computer);
        postResponse.EnsureSuccessStatusCode();

        var assets = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Computer&search=fleet-auto-pc", JsonOpts);
        Assert.True(assets.GetArrayLength() > 0);

        var asset = assets.EnumerateArray().First(a => a.GetProperty("name").GetString() == "fleet-auto-pc");
        Assert.Equal("Computer", asset.GetProperty("type").GetString());
        Assert.Equal("InUse", asset.GetProperty("status").GetString());
        Assert.Equal("Agent", asset.GetProperty("source").GetString());
        Assert.Equal("FLEET-SN-001", asset.GetProperty("serialNumber").GetString());
    }

    [Fact]
    public async Task MultipleComputers_AllReturnedCorrectly()
    {
        var compA = TestFixtures.CreateTestComputer(name: "fleet-multi-a", uuid: "fleet-multi-uuid-a");
        var compB = TestFixtures.CreateTestComputer(name: "fleet-multi-b", uuid: "fleet-multi-uuid-b");

        await _client.PostAsJsonAsync("/inventory/computer", compA);
        await _client.PostAsJsonAsync("/inventory/computer", compB);

        // Both should appear in computer list
        var computers = await _client.GetFromJsonAsync<JsonElement>("/inventory/computers", JsonOpts);
        var names = computers.EnumerateArray().Select(c => c.GetProperty("computerName").GetString()).ToList();
        Assert.Contains("fleet-multi-a", names);
        Assert.Contains("fleet-multi-b", names);

        // Both should have auto-created assets
        var assetsA = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Computer&search=fleet-multi-a", JsonOpts);
        var assetsB = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Computer&search=fleet-multi-b", JsonOpts);
        Assert.True(assetsA.GetArrayLength() > 0);
        Assert.True(assetsB.GetArrayLength() > 0);
    }

    [Fact]
    public async Task ComputerReReport_UpdatesDataPreservesAsset()
    {
        var original = TestFixtures.CreateTestComputer(
            name: "fleet-rereport-pc",
            uuid: "fleet-rereport-uuid",
            serialNumber: "FLEET-SN-REREPORT",
            disks: [new DiskInfo("Macintosh HD", "APFS", 1_000_000_000_000, 500_000_000_000)]);

        await _client.PostAsJsonAsync("/inventory/computer", original);

        // Capture the asset ID
        var assets1 = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Computer&search=fleet-rereport-pc", JsonOpts);
        var assetId = assets1.EnumerateArray().First().GetProperty("id").GetString();

        // Re-report with changed disk data
        var updated = TestFixtures.CreateTestComputer(
            name: "fleet-rereport-pc",
            uuid: "fleet-rereport-uuid",
            serialNumber: "FLEET-SN-REREPORT",
            disks: [new DiskInfo("Updated SSD", "APFS", 2_000_000_000_000, 1_000_000_000_000)]);

        await _client.PostAsJsonAsync("/inventory/computer", updated);

        // Verify computer data is updated
        var comp = await _client.GetFromJsonAsync<JsonElement>("/inventory/computers/fleet-rereport-pc", JsonOpts);
        var disks = comp.GetProperty("data").GetProperty("disks").EnumerateArray().ToList();
        Assert.Single(disks);
        Assert.Equal("Updated SSD", disks[0].GetProperty("name").GetString());
        Assert.Equal(2_000_000_000_000, disks[0].GetProperty("totalBytes").GetInt64());

        // Verify asset ID is the same (not duplicated)
        var assets2 = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Computer&search=fleet-rereport-pc", JsonOpts);
        var matchingAssets = assets2.EnumerateArray()
            .Where(a => a.GetProperty("name").GetString() == "fleet-rereport-pc")
            .ToList();
        Assert.Single(matchingAssets);
        Assert.Equal(assetId, matchingAssets[0].GetProperty("id").GetString());
    }
}
