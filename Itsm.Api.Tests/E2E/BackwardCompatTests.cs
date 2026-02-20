using System.Net.Http.Json;
using System.Text.Json;
using Itsm.Common.Models;

namespace Itsm.Api.Tests.E2E;

public class BackwardCompatTests : IClassFixture<ItsmApiFactory>, IDisposable
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public BackwardCompatTests(ItsmApiFactory factory) => _client = factory.CreateClient();
    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task ComputerResponse_MatchesLegacyJsonbFormat()
    {
        var lastBoot = DateTime.UtcNow.AddHours(-12);
        var computer = new Computer(
            Identity: new MachineIdentity(
                "compat-test-pc",
                "MacBook Pro 16-inch 2023",
                "C02ZZ1234567",
                "compat-hw-uuid",
                "jdoe",
                ChassisType.Laptop),
            Cpu: new CpuInfo("Apple M2 Max", 12, "arm64"),
            Memory: new MemoryInfo(68_719_476_736),
            Disks: [
                new DiskInfo("Macintosh HD", "APFS", 2_000_000_000_000, 800_000_000_000),
                new DiskInfo("Data Volume", "APFS", 500_000_000_000, 200_000_000_000)
            ],
            Os: new OsInfo("macOS 14.3 Sonoma", "14.3", "23D56"),
            Network: new NetworkInfo("compat-test-pc.local", [
                new NetworkInterfaceInfo("en0", "AA:BB:CC:DD:EE:01", ["192.168.1.100", "fe80::1"]),
                new NetworkInterfaceInfo("en1", "AA:BB:CC:DD:EE:02", ["10.0.0.50"])
            ]),
            Gpus: [
                new GpuInfo("Apple M2 Max GPU", "Apple", 0, null),
                new GpuInfo("AMD Radeon Pro W6800X", "AMD", 32_000_000_000, "21.40.01")
            ],
            Battery: new BatteryInfo(true, 72.5, 245, 89.3, false, "Normal"),
            InstalledApps: [
                new InstalledApp("Visual Studio Code", "1.86.0", "2024-02-01"),
                new InstalledApp("Slack", "4.36.140", "2024-01-20"),
                new InstalledApp("Docker Desktop", "4.27.1", null)
            ],
            Uptime: new UptimeInfo(lastBoot, TimeSpan.FromHours(12)),
            Firewall: new FirewallInfo(true, true),
            Encryption: new EncryptionInfo(true, "FileVault")
        );

        // POST
        var postResponse = await _client.PostAsJsonAsync("/inventory/computer", computer);
        postResponse.EnsureSuccessStatusCode();

        // GET by name
        var response = await _client.GetAsync("/inventory/computers/compat-test-pc");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        // Top-level fields
        Assert.Equal("compat-test-pc", json.GetProperty("computerName").GetString());
        Assert.True(json.TryGetProperty("lastUpdatedUtc", out _));

        var data = json.GetProperty("data");

        // Identity
        var identity = data.GetProperty("identity");
        Assert.Equal("compat-test-pc", identity.GetProperty("computerName").GetString());
        Assert.Equal("MacBook Pro 16-inch 2023", identity.GetProperty("modelName").GetString());
        Assert.Equal("C02ZZ1234567", identity.GetProperty("serialNumber").GetString());
        Assert.Equal("compat-hw-uuid", identity.GetProperty("hardwareUuid").GetString());
        Assert.Equal("jdoe", identity.GetProperty("loggedInUser").GetString());
        Assert.Equal("Laptop", identity.GetProperty("chassisType").GetString());

        // CPU
        var cpu = data.GetProperty("cpu");
        Assert.Equal("Apple M2 Max", cpu.GetProperty("brandString").GetString());
        Assert.Equal(12, cpu.GetProperty("coreCount").GetInt32());
        Assert.Equal("arm64", cpu.GetProperty("architecture").GetString());

        // Memory
        Assert.Equal(68_719_476_736, data.GetProperty("memory").GetProperty("totalBytes").GetInt64());

        // Disks
        var disks = data.GetProperty("disks").EnumerateArray().ToList();
        Assert.Equal(2, disks.Count);
        Assert.Equal("Macintosh HD", disks[0].GetProperty("name").GetString());
        Assert.Equal("APFS", disks[0].GetProperty("format").GetString());
        Assert.Equal(2_000_000_000_000, disks[0].GetProperty("totalBytes").GetInt64());
        Assert.Equal(800_000_000_000, disks[0].GetProperty("freeBytes").GetInt64());
        Assert.Equal("Data Volume", disks[1].GetProperty("name").GetString());

        // OS
        var os = data.GetProperty("os");
        Assert.Equal("macOS 14.3 Sonoma", os.GetProperty("description").GetString());
        Assert.Equal("14.3", os.GetProperty("version").GetString());
        Assert.Equal("23D56", os.GetProperty("buildNumber").GetString());

        // Network
        var network = data.GetProperty("network");
        Assert.Equal("compat-test-pc.local", network.GetProperty("hostname").GetString());
        var interfaces = network.GetProperty("interfaces").EnumerateArray().ToList();
        Assert.Equal(2, interfaces.Count);
        Assert.Equal("en0", interfaces[0].GetProperty("name").GetString());
        Assert.Equal("AA:BB:CC:DD:EE:01", interfaces[0].GetProperty("macAddress").GetString());
        var ips = interfaces[0].GetProperty("ipAddresses").EnumerateArray().Select(ip => ip.GetString()).ToList();
        Assert.Contains("192.168.1.100", ips);
        Assert.Contains("fe80::1", ips);
        Assert.Equal("en1", interfaces[1].GetProperty("name").GetString());

        // GPUs
        var gpus = data.GetProperty("gpus").EnumerateArray().ToList();
        Assert.Equal(2, gpus.Count);
        Assert.Equal("Apple M2 Max GPU", gpus[0].GetProperty("name").GetString());
        Assert.Equal("Apple", gpus[0].GetProperty("vendor").GetString());
        Assert.Equal("AMD Radeon Pro W6800X", gpus[1].GetProperty("name").GetString());
        Assert.Equal("AMD", gpus[1].GetProperty("vendor").GetString());
        Assert.Equal(32_000_000_000, gpus[1].GetProperty("vramBytes").GetInt64());
        Assert.Equal("21.40.01", gpus[1].GetProperty("driverVersion").GetString());

        // Battery
        var battery = data.GetProperty("battery");
        Assert.True(battery.GetProperty("isPresent").GetBoolean());
        Assert.Equal(72.5, battery.GetProperty("chargePercent").GetDouble());
        Assert.Equal(245, battery.GetProperty("cycleCount").GetInt32());
        Assert.Equal(89.3, battery.GetProperty("healthPercent").GetDouble());
        Assert.False(battery.GetProperty("isCharging").GetBoolean());
        Assert.Equal("Normal", battery.GetProperty("condition").GetString());

        // Installed Apps
        var apps = data.GetProperty("installedApps").EnumerateArray().ToList();
        Assert.Equal(3, apps.Count);
        var appNames = apps.Select(a => a.GetProperty("name").GetString()).ToList();
        Assert.Contains("Visual Studio Code", appNames);
        Assert.Contains("Slack", appNames);
        Assert.Contains("Docker Desktop", appNames);

        var vscode = apps.First(a => a.GetProperty("name").GetString() == "Visual Studio Code");
        Assert.Equal("1.86.0", vscode.GetProperty("version").GetString());
        Assert.Equal("2024-02-01", vscode.GetProperty("installDate").GetString());

        var docker = apps.First(a => a.GetProperty("name").GetString() == "Docker Desktop");
        Assert.Equal("4.27.1", docker.GetProperty("version").GetString());

        // Uptime
        var uptime = data.GetProperty("uptime");
        Assert.True(uptime.TryGetProperty("lastBootUtc", out _));
        Assert.True(uptime.TryGetProperty("uptime", out _));

        // Firewall
        var firewall = data.GetProperty("firewall");
        Assert.True(firewall.GetProperty("isEnabled").GetBoolean());
        Assert.True(firewall.GetProperty("stealthMode").GetBoolean());

        // Encryption
        var encryption = data.GetProperty("encryption");
        Assert.True(encryption.GetProperty("isEnabled").GetBoolean());
        Assert.Equal("FileVault", encryption.GetProperty("method").GetString());
    }
}
