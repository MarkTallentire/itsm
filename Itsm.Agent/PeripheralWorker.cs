using System.Net.Http.Json;
using Itsm.Common.Models;

namespace Itsm.Agent;

public class PeripheralWorker(
    ILogger<PeripheralWorker> logger,
    IPeripheralGatherer peripheralGatherer,
    INetworkPrinterScanner printerScanner,
    IHardwareGatherer hardwareGatherer,
    IHttpClientFactory httpClientFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Starting peripheral discovery");

                var identity = hardwareGatherer.GetMachineIdentity();
                var monitors = peripheralGatherer.GetMonitors();
                var usbDevices = peripheralGatherer.GetUsbDevices();
                var client = httpClientFactory.CreateClient("itsm-api");

                // Post monitors + USB immediately (these are fast, local-only)
                if (monitors.Count > 0 || usbDevices.Count > 0)
                {
                    var localReport = new PeripheralReport(
                        identity.HardwareUuid,
                        identity.ComputerName,
                        monitors,
                        usbDevices,
                        []);

                    var localResponse = await client.PostAsJsonAsync("/inventory/peripherals", localReport, stoppingToken);
                    logger.LogInformation(
                        "Posted local peripherals — {MonitorCount} monitors, {UsbCount} USB devices — status: {Status}",
                        monitors.Count, usbDevices.Count, localResponse.StatusCode);
                }

                // Printer scan is slow (network SNMP) — runs separately
                var printers = await printerScanner.ScanAsync(stoppingToken);
                if (printers.Count > 0)
                {
                    var printerReport = new PeripheralReport(
                        identity.HardwareUuid,
                        identity.ComputerName,
                        [],
                        [],
                        printers);

                    var printerResponse = await client.PostAsJsonAsync("/inventory/peripherals", printerReport, stoppingToken);
                    logger.LogInformation(
                        "Posted network printers — {PrinterCount} printers — status: {Status}",
                        printers.Count, printerResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to post peripherals to API");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
