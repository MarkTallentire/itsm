using System.Net.Http.Json;
using Itsm.Common.Models;

namespace Itsm.Agent;

public class DiskUsageWorker(
    ILogger<DiskUsageWorker> logger,
    IDiskUsageScanner scanner,
    IHttpClientFactory httpClientFactory) : BackgroundService
{
    private const long DefaultMinimumSizeBytes = 100 * 1024 * 1024; // 100 MB

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Starting disk usage scan");
                var snapshot = scanner.Scan(DefaultMinimumSizeBytes);

                var client = httpClientFactory.CreateClient("itsm-api");
                var response = await client.PostAsJsonAsync("/inventory/disk-usage", snapshot, stoppingToken);
                var body = await response.Content.ReadAsStringAsync(stoppingToken);

                logger.LogInformation("Posted disk usage to API — status: {Status}, response: {Body}", response.StatusCode, body);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to post disk usage to API");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
