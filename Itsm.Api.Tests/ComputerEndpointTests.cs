using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Itsm.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Itsm.Api.Tests;

public class ComputerEndpointTests : IClassFixture<ItsmApiFactory>, IDisposable
{
    private readonly ItsmApiFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ComputerEndpointTests(ItsmApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task PostComputer_CreatesComputerAndAsset()
    {
        var computer = TestFixtures.CreateTestComputer(name: "post-test-pc", uuid: "uuid-post-1");

        var response = await _client.PostAsJsonAsync("/inventory/computer", computer);
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        var entity = await db.Computers.Include(c => c.Asset).FirstOrDefaultAsync(c => c.ComputerName == "post-test-pc");
        Assert.NotNull(entity);
        Assert.Equal("MacBook Pro 16", entity.ModelName);
        Assert.Equal("uuid-post-1", entity.HardwareUuid);
        Assert.Equal("Apple M2 Pro", entity.CpuBrand);
        Assert.Equal(12, entity.CpuCores);
        Assert.Equal(34359738368, entity.TotalMemoryBytes);
        Assert.True(entity.FirewallEnabled);
        Assert.True(entity.EncryptionEnabled);
        Assert.Equal("FileVault", entity.EncryptionMethod);

        // Verify asset was created
        Assert.NotNull(entity.Asset);
        Assert.Equal("post-test-pc", entity.Asset.Name);
        Assert.Equal("Computer", entity.Asset.Type);
        Assert.Equal("InUse", entity.Asset.Status);
        Assert.Equal("Agent", entity.Asset.Source);
    }

    [Fact]
    public async Task PostComputer_PopulatesChildTables()
    {
        var computer = TestFixtures.CreateTestComputer(name: "child-test-pc", uuid: "uuid-child-1");

        await _client.PostAsJsonAsync("/inventory/computer", computer);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        var entity = await db.Computers
            .Include(c => c.Disks)
            .Include(c => c.NetworkInterfaces)
            .Include(c => c.ComputerGpus)
            .Include(c => c.ComputerSoftware)
            .FirstOrDefaultAsync(c => c.ComputerName == "child-test-pc");

        Assert.NotNull(entity);
        Assert.Single(entity.Disks);
        Assert.Equal("Macintosh HD", entity.Disks[0].Name);
        Assert.Single(entity.NetworkInterfaces);
        Assert.Equal("AA:BB:CC:DD:EE:FF", entity.NetworkInterfaces[0].MacAddress);
        Assert.Single(entity.ComputerGpus);
        Assert.Single(entity.ComputerSoftware);
    }

    [Fact]
    public async Task PostComputer_Upserts_NotDuplicates()
    {
        var computer1 = TestFixtures.CreateTestComputer(name: "upsert-pc", uuid: "uuid-upsert-1");
        var computer2 = TestFixtures.CreateTestComputer(name: "upsert-pc", uuid: "uuid-upsert-1",
            disks: [new DiskInfo("Updated Disk", "APFS", 2000000000000, 1000000000000)]);

        await _client.PostAsJsonAsync("/inventory/computer", computer1);
        await _client.PostAsJsonAsync("/inventory/computer", computer2);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        var computers = await db.Computers.Where(c => c.ComputerName == "upsert-pc").ToListAsync();
        Assert.Single(computers);

        var entity = await db.Computers.Include(c => c.Disks).FirstAsync(c => c.ComputerName == "upsert-pc");
        Assert.Single(entity.Disks);
        Assert.Equal("Updated Disk", entity.Disks[0].Name);
    }

    [Fact]
    public async Task PostComputer_GpuDeduplication()
    {
        var sharedGpu = new GpuInfo("NVIDIA RTX 4090", "NVIDIA", 24_000_000_000, "535.129.03");

        var computer1 = TestFixtures.CreateTestComputer(name: "gpu-pc-1", uuid: "uuid-gpu-1", gpus: [sharedGpu]);
        var computer2 = TestFixtures.CreateTestComputer(name: "gpu-pc-2", uuid: "uuid-gpu-2", gpus: [sharedGpu]);

        await _client.PostAsJsonAsync("/inventory/computer", computer1);
        await _client.PostAsJsonAsync("/inventory/computer", computer2);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        var gpuModels = await db.GpuModels.Where(g => g.Name == "NVIDIA RTX 4090").ToListAsync();
        Assert.Single(gpuModels);

        var computerGpus = await db.ComputerGpus.Where(cg => cg.GpuModelId == gpuModels[0].Id).ToListAsync();
        Assert.Equal(2, computerGpus.Count);
    }

    [Fact]
    public async Task PostComputer_SoftwareDeduplication()
    {
        var sharedApp = new InstalledApp("Google Chrome", "120.0.6099.129", "2024-01-10");

        var computer1 = TestFixtures.CreateTestComputer(name: "sw-pc-1", uuid: "uuid-sw-1", apps: [sharedApp]);
        var computer2 = TestFixtures.CreateTestComputer(name: "sw-pc-2", uuid: "uuid-sw-2", apps: [sharedApp]);

        await _client.PostAsJsonAsync("/inventory/computer", computer1);
        await _client.PostAsJsonAsync("/inventory/computer", computer2);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        var titles = await db.SoftwareTitles.Where(s => s.Name == "Google Chrome").ToListAsync();
        Assert.Single(titles);

        var computerSw = await db.ComputerSoftware.Where(cs => cs.SoftwareTitleId == titles[0].Id).ToListAsync();
        Assert.Equal(2, computerSw.Count);
    }

    [Fact]
    public async Task GetComputers_ReturnsAll()
    {
        var computer = TestFixtures.CreateTestComputer(name: "list-pc", uuid: "uuid-list-1");
        await _client.PostAsJsonAsync("/inventory/computer", computer);

        var response = await _client.GetAsync("/inventory/computers");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);

        var found = json.EnumerateArray().Any(e =>
            e.GetProperty("computerName").GetString() == "list-pc");
        Assert.True(found);
    }

    [Fact]
    public async Task GetComputers_ResponseHasExpectedWireFormat()
    {
        var computer = TestFixtures.CreateTestComputer(name: "wire-pc", uuid: "uuid-wire-1");
        await _client.PostAsJsonAsync("/inventory/computer", computer);

        var response = await _client.GetAsync("/inventory/computers");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        var record = json.EnumerateArray().First(e =>
            e.GetProperty("computerName").GetString() == "wire-pc");

        // Verify top-level shape
        Assert.True(record.TryGetProperty("computerName", out _));
        Assert.True(record.TryGetProperty("lastUpdatedUtc", out _));
        Assert.True(record.TryGetProperty("data", out var data));

        // Verify nested data sections
        Assert.True(data.TryGetProperty("identity", out var identity));
        Assert.Equal("wire-pc", identity.GetProperty("computerName").GetString());
        Assert.Equal("MacBook Pro 16", identity.GetProperty("modelName").GetString());

        Assert.True(data.TryGetProperty("cpu", out var cpu));
        Assert.Equal("Apple M2 Pro", cpu.GetProperty("brandString").GetString());

        Assert.True(data.TryGetProperty("memory", out _));
        Assert.True(data.TryGetProperty("disks", out _));
        Assert.True(data.TryGetProperty("os", out _));
        Assert.True(data.TryGetProperty("network", out _));
        Assert.True(data.TryGetProperty("gpus", out _));
        Assert.True(data.TryGetProperty("battery", out _));
        Assert.True(data.TryGetProperty("installedApps", out _));
        Assert.True(data.TryGetProperty("uptime", out _));
        Assert.True(data.TryGetProperty("firewall", out _));
        Assert.True(data.TryGetProperty("encryption", out _));
    }

    [Fact]
    public async Task GetComputerByName_ReturnsComputer()
    {
        var computer = TestFixtures.CreateTestComputer(name: "byname-pc", uuid: "uuid-byname-1");
        await _client.PostAsJsonAsync("/inventory/computer", computer);

        var response = await _client.GetAsync("/inventory/computers/byname-pc");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("byname-pc", json.GetProperty("computerName").GetString());
    }

    [Fact]
    public async Task GetComputerByName_Returns404ForMissing()
    {
        var response = await _client.GetAsync("/inventory/computers/nonexistent-pc-xyz");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
