using System.Net.Http.Json;
using Itsm.Common.Models;

namespace Itsm.Agent;

public class Worker(
    ILogger<Worker> logger,
    IHardwareGatherer hardwareGatherer,
    IHttpClientFactory httpClientFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
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
                    Encryption: hardwareGatherer.GetEncryptionInformation());

                var client = httpClientFactory.CreateClient("itsm-api");
                var response = await client.PostAsJsonAsync("/inventory/computer", computer, stoppingToken);
                var body = await response.Content.ReadAsStringAsync(stoppingToken);

                logger.LogInformation("Posted inventory to API — status: {Status}, response: {Body}", response.StatusCode, body);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to post inventory to API");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
