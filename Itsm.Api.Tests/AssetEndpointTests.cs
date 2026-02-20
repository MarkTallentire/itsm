using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Itsm.Api.Tests;

public class AssetEndpointTests : IClassFixture<ItsmApiFactory>, IDisposable
{
    private readonly ItsmApiFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AssetEndpointTests(ItsmApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task PostAsset_CreatesManualAsset()
    {
        var asset = new
        {
            Name = "Test Laptop",
            Type = "Computer",
            Status = "InUse",
            SerialNumber = "MANUAL-SN-001",
            AssignedUser = "john.doe",
            Location = "Office A",
            Notes = "Test asset"
        };

        var response = await _client.PostAsJsonAsync("/assets", asset);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Test Laptop", created.GetProperty("name").GetString());
        Assert.Equal("Manual", created.GetProperty("source").GetString());
        Assert.NotEqual(Guid.Empty.ToString(), created.GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetAssets_ReturnsAll()
    {
        // Seed an asset
        await _client.PostAsJsonAsync("/assets", new { Name = "List Asset 1", Type = "Computer", Status = "InUse" });

        var response = await _client.GetAsync("/assets");
        response.EnsureSuccessStatusCode();

        var assets = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(JsonValueKind.Array, assets.ValueKind);
        Assert.True(assets.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetAssets_FiltersByType()
    {
        await _client.PostAsJsonAsync("/assets", new { Name = "Phone Asset", Type = "Phone", Status = "InUse" });
        await _client.PostAsJsonAsync("/assets", new { Name = "Monitor Asset", Type = "Monitor", Status = "InUse" });

        var response = await _client.GetAsync("/assets?type=Phone");
        var assets = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        foreach (var asset in assets.EnumerateArray())
            Assert.Equal("Phone", asset.GetProperty("type").GetString());
    }

    [Fact]
    public async Task GetAssets_FiltersByStatus()
    {
        await _client.PostAsJsonAsync("/assets", new { Name = "Stored Asset", Type = "Other", Status = "InStorage" });

        var response = await _client.GetAsync("/assets?status=InStorage");
        var assets = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        foreach (var asset in assets.EnumerateArray())
            Assert.Equal("InStorage", asset.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetAssets_SearchesByName()
    {
        await _client.PostAsJsonAsync("/assets", new { Name = "Searchable Widget", Type = "Other", Status = "InUse" });

        var response = await _client.GetAsync("/assets?search=searchable");
        var assets = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        Assert.True(assets.GetArrayLength() > 0);
        var found = assets.EnumerateArray().Any(a =>
            a.GetProperty("name").GetString()!.Contains("Searchable"));
        Assert.True(found);
    }

    [Fact]
    public async Task GetAssets_SearchesBySerialNumber()
    {
        await _client.PostAsJsonAsync("/assets", new { Name = "SN Search Asset", Type = "Other", Status = "InUse", SerialNumber = "UNIQUE-SN-9999" });

        var response = await _client.GetAsync("/assets?search=unique-sn-9999");
        var assets = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        Assert.True(assets.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetAssets_SearchesByAssignedUser()
    {
        await _client.PostAsJsonAsync("/assets", new { Name = "User Search Asset", Type = "Other", Status = "InUse", AssignedUser = "jane.specific" });

        var response = await _client.GetAsync("/assets?search=jane.specific");
        var assets = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        Assert.True(assets.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetAssetById_ReturnsAsset()
    {
        var createResponse = await _client.PostAsJsonAsync("/assets", new { Name = "ById Asset", Type = "Other", Status = "InUse" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var id = created.GetProperty("id").GetString();

        var response = await _client.GetAsync($"/assets/{id}");
        response.EnsureSuccessStatusCode();

        var asset = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("ById Asset", asset.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetAssetById_Returns404ForMissing()
    {
        var response = await _client.GetAsync($"/assets/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutAsset_UpdatesFields()
    {
        var createResponse = await _client.PostAsJsonAsync("/assets", new { Name = "Update Me", Type = "Other", Status = "InUse" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var id = created.GetProperty("id").GetString();

        var updated = new
        {
            Name = "Updated Name",
            Status = "InStorage",
            AssignedUser = "new.user",
            Location = "Building B",
            Notes = "Updated notes",
            Type = "Other"
        };

        var response = await _client.PutAsJsonAsync($"/assets/{id}", updated);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Updated Name", result.GetProperty("name").GetString());
        Assert.Equal("InStorage", result.GetProperty("status").GetString());
        Assert.Equal("new.user", result.GetProperty("assignedUser").GetString());
        Assert.Equal("Building B", result.GetProperty("location").GetString());
    }

    [Fact]
    public async Task PutAsset_Returns404ForMissing()
    {
        var response = await _client.PutAsJsonAsync($"/assets/{Guid.NewGuid()}", new { Name = "X", Type = "Other", Status = "InUse" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAsset_RemovesAsset()
    {
        var createResponse = await _client.PostAsJsonAsync("/assets", new { Name = "Delete Me", Type = "Other", Status = "InUse" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var id = created.GetProperty("id").GetString();

        var deleteResponse = await _client.DeleteAsync($"/assets/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/assets/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteAsset_Returns404ForMissing()
    {
        var response = await _client.DeleteAsync($"/assets/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ComputerPost_AutoCreatesAsset()
    {
        var computer = TestFixtures.CreateTestComputer(name: "auto-asset-pc", uuid: "uuid-auto-asset");
        await _client.PostAsJsonAsync("/inventory/computer", computer);

        var response = await _client.GetAsync("/assets?type=Computer&search=auto-asset-pc");
        var assets = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        Assert.True(assets.GetArrayLength() > 0);
        var asset = assets.EnumerateArray().First();
        Assert.Equal("Computer", asset.GetProperty("type").GetString());
        Assert.Equal("Agent", asset.GetProperty("source").GetString());
    }
}
