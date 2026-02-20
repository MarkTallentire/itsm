using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Itsm.Api.Tests.E2E;

public class ManualAssetTests : IClassFixture<ItsmApiFactory>, IDisposable
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ManualAssetTests(ItsmApiFactory factory) => _client = factory.CreateClient();
    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task CreateReadUpdateDelete_FullLifecycle()
    {
        // Create
        var createResponse = await _client.PostAsJsonAsync("/assets", new
        {
            Name = "iPhone 15 E2E",
            Type = "Phone",
            Status = "InUse",
            SerialNumber = "IPHONE-E2E-001",
            AssignedUser = "alice",
            Location = "HQ"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var id = created.GetProperty("id").GetString()!;
        Assert.Equal("iPhone 15 E2E", created.GetProperty("name").GetString());
        Assert.Equal("Manual", created.GetProperty("source").GetString());

        // Read
        var getResponse = await _client.GetAsync($"/assets/{id}");
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("iPhone 15 E2E", fetched.GetProperty("name").GetString());
        Assert.Equal("Phone", fetched.GetProperty("type").GetString());
        Assert.Equal("alice", fetched.GetProperty("assignedUser").GetString());

        // Update
        var updateResponse = await _client.PutAsJsonAsync($"/assets/{id}", new
        {
            Name = "iPhone 15 E2E",
            Type = "Phone",
            Status = "Decommissioned",
            Notes = "Screen cracked",
            AssignedUser = "alice",
            Location = "Storage Room"
        });
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Decommissioned", updated.GetProperty("status").GetString());
        Assert.Equal("Screen cracked", updated.GetProperty("notes").GetString());
        Assert.Equal("Storage Room", updated.GetProperty("location").GetString());

        // Verify via GET
        var verifyResponse = await _client.GetAsync($"/assets/{id}");
        var verified = await verifyResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Decommissioned", verified.GetProperty("status").GetString());

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/assets/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify 404
        var notFoundResponse = await _client.GetAsync($"/assets/{id}");
        Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
    }

    [Fact]
    public async Task AssetFiltering_WorksCorrectly()
    {
        // Create several assets of different types and statuses
        await _client.PostAsJsonAsync("/assets", new { Name = "E2E Phone 1", Type = "Phone", Status = "InUse" });
        await _client.PostAsJsonAsync("/assets", new { Name = "E2E Phone 2", Type = "Phone", Status = "Decommissioned" });
        await _client.PostAsJsonAsync("/assets", new { Name = "E2E Tablet 1", Type = "Tablet", Status = "InUse" });
        await _client.PostAsJsonAsync("/assets", new { Name = "E2E Other Decom", Type = "Other", Status = "Decommissioned" });

        // Filter by type
        var phones = await _client.GetFromJsonAsync<JsonElement>("/assets?type=Phone", JsonOpts);
        foreach (var phone in phones.EnumerateArray())
            Assert.Equal("Phone", phone.GetProperty("type").GetString());
        Assert.True(phones.GetArrayLength() >= 2);

        // Filter by status
        var decom = await _client.GetFromJsonAsync<JsonElement>("/assets?status=Decommissioned", JsonOpts);
        foreach (var asset in decom.EnumerateArray())
            Assert.Equal("Decommissioned", asset.GetProperty("status").GetString());
        Assert.True(decom.GetArrayLength() >= 2);

        // Search by name
        var search = await _client.GetFromJsonAsync<JsonElement>("/assets?search=E2E Tablet", JsonOpts);
        Assert.True(search.GetArrayLength() >= 1);
        var found = search.EnumerateArray().Any(a => a.GetProperty("name").GetString() == "E2E Tablet 1");
        Assert.True(found);
    }
}
