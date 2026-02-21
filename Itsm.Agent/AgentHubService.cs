using System.Net.Http.Json;
using System.Reflection;
using Itsm.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Itsm.Agent;

public class AgentHubService(
    ILogger<AgentHubService> logger,
    IConfiguration configuration,
    IHardwareGatherer hardwareGatherer,
    IDiskUsageScanner diskUsageScanner,
    IPeripheralGatherer peripheralGatherer,
    INetworkPrinterScanner printerScanner,
    IHttpClientFactory httpClientFactory,
    HubLoggerProvider hubLoggerProvider) : BackgroundService
{
    private const long DefaultMinimumSizeBytes = 100 * 1024 * 1024;
    private HubConnection? _connection;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hubUrl = GetHubUrl();
        logger.LogInformation("Connecting to agent hub at {Url}", hubUrl);

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<UpdateType>("RequestUpdate", async updateType =>
        {
            logger.LogInformation("Received update request: {UpdateType}", updateType);
            try
            {
                var client = httpClientFactory.CreateClient("itsm-api");
                if (updateType == UpdateType.Inventory)
                {
                    var computer = new Computer(
                        Identity: hardwareGatherer.GetMachineIdentity(),
                        Cpu: hardwareGatherer.GetCpuInformation(),
                        Memory: hardwareGatherer.GetMemoryInformation(),
                        Disks: hardwareGatherer.GetDiskInformation(),
                        Os: hardwareGatherer.GetOsInformation(),
                        Network: hardwareGatherer.GetNetworkInformation(),
                        Gpus: hardwareGatherer.GetGpuInformation(),
                        Battery: hardwareGatherer.GetBatteryInformation(),
                        InstalledApps: hardwareGatherer.GetInstalledApplications(),
                        Uptime: hardwareGatherer.GetUptimeInformation(),
                        Firewall: hardwareGatherer.GetFirewallInformation(),
                        Encryption: hardwareGatherer.GetEncryptionInformation(),
                        Bios: hardwareGatherer.GetBiosInformation(),
                        Motherboard: hardwareGatherer.GetMotherboardInformation(),
                        Antivirus: hardwareGatherer.GetAntivirusInformation(),
                        Controllers: hardwareGatherer.GetControllers(),
                        Virtualization: hardwareGatherer.GetVirtualizationInformation(),
                        Databases: hardwareGatherer.GetDatabaseInstances(),
                        Location: await hardwareGatherer.GetLocationAsync());
                    await client.PostAsJsonAsync("/inventory/computer", computer);
                    logger.LogInformation("Inventory update posted successfully");
                }
                else if (updateType == UpdateType.DiskUsage)
                {
                    var snapshot = diskUsageScanner.Scan(DefaultMinimumSizeBytes);
                    await client.PostAsJsonAsync("/inventory/disk-usage", snapshot);
                    logger.LogInformation("Disk usage update posted successfully");
                }
                else if (updateType == UpdateType.Peripherals)
                {
                    var identity = hardwareGatherer.GetMachineIdentity();
                    var report = new PeripheralReport(
                        identity.HardwareUuid,
                        identity.ComputerName,
                        peripheralGatherer.GetMonitors(),
                        peripheralGatherer.GetUsbDevices(),
                        await printerScanner.ScanAsync());
                    await client.PostAsJsonAsync("/inventory/peripherals", report);
                    logger.LogInformation("Peripheral update posted successfully");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process update request: {UpdateType}", updateType);
            }
        });

        _connection.Reconnected += async _ =>
        {
            logger.LogInformation("Reconnected to agent hub, re-registering");
            await RegisterAsync();
        };

        // Retry connection loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _connection.StartAsync(stoppingToken);
                hubLoggerProvider.SetConnection(_connection);
                logger.LogInformation("Connected to agent hub");
                await RegisterAsync();
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to connect to agent hub, retrying in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        // Keep alive until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private async Task RegisterAsync()
    {
        if (_connection is null) return;
        var identity = hardwareGatherer.GetMachineIdentity();
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        await _connection.InvokeAsync("Register", identity.HardwareUuid, identity.ComputerName, version);
        logger.LogInformation("Registered as {HardwareUuid} ({ComputerName}) v{Version}", identity.HardwareUuid, identity.ComputerName, version);
    }

    private string GetHubUrl()
    {
        // Try Aspire service discovery via IConfiguration (handles both hyphen and underscore resource names)
        var baseUrl = configuration["services:itsm-api:https:0"]
                   ?? configuration["services:itsm-api:http:0"]
                   ?? configuration["services:itsm_api:https:0"]
                   ?? configuration["services:itsm_api:http:0"]
                   ?? "http://localhost:5119";
        logger.LogInformation("Resolved API base URL: {BaseUrl}", baseUrl);
        return $"{baseUrl.TrimEnd('/')}/hubs/agent";
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
