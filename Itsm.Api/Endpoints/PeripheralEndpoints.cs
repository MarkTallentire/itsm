using Itsm.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Itsm.Api.Endpoints;

public static class PeripheralEndpoints
{
    public static void MapPeripheralEndpoints(this WebApplication app)
    {
        app.MapPost("/inventory/peripherals", async (PeripheralReport report, ItsmDbContext db) =>
        {
            var now = DateTime.UtcNow;

            // ── Monitors ──
            foreach (var monitor in report.Monitors)
            {
                // Find-or-create monitor model
                var model = await db.MonitorModels
                    .FirstOrDefaultAsync(m => m.Manufacturer == monitor.Manufacturer && m.ModelName == monitor.ModelName)
                    ?? db.MonitorModels.Local.FirstOrDefault(m => m.Manufacturer == monitor.Manufacturer && m.ModelName == monitor.ModelName);
                if (model is null)
                {
                    model = new MonitorModelEntity
                    {
                        Id = Guid.NewGuid(),
                        Manufacturer = monitor.Manufacturer,
                        ModelName = monitor.ModelName
                    };
                    db.MonitorModels.Add(model);
                }

                // Update resolution/size if provided
                if (monitor.WidthPixels.HasValue) model.WidthPixels = monitor.WidthPixels;
                if (monitor.HeightPixels.HasValue) model.HeightPixels = monitor.HeightPixels;
                if (monitor.DiagonalInches.HasValue) model.DiagonalInches = monitor.DiagonalInches;

                // Find existing asset by serial number, or create new
                AssetRecord? asset = null;
                MonitorEntity? existing = null;

                if (!string.IsNullOrEmpty(monitor.SerialNumber))
                {
                    asset = await db.Assets.FirstOrDefaultAsync(a =>
                        a.SerialNumber == monitor.SerialNumber && a.Type == nameof(AssetType.Monitor));
                    if (asset != null)
                        existing = await db.Monitors.FirstOrDefaultAsync(m => m.Id == asset.Id);
                }

                if (asset is null)
                {
                    asset = new AssetRecord
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{monitor.Manufacturer} {monitor.ModelName}",
                        Type = nameof(AssetType.Monitor),
                        Status = nameof(AssetStatus.InUse),
                        SerialNumber = monitor.SerialNumber,
                        Source = nameof(DiscoverySource.Agent),
                        DiscoveredByAgent = report.HardwareUuid,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    };
                    db.Assets.Add(asset);
                }
                else
                {
                    // Preserve user-edited fields; update agent-sourced fields
                    asset.Name = $"{monitor.Manufacturer} {monitor.ModelName}";
                    asset.DiscoveredByAgent = report.HardwareUuid;
                    asset.UpdatedAtUtc = now;
                }

                if (existing is null)
                {
                    db.Monitors.Add(new MonitorEntity
                    {
                        Id = asset.Id,
                        MonitorModelId = model.Id,
                        ManufactureYear = monitor.ManufactureYear
                    });
                }
                else
                {
                    existing.MonitorModelId = model.Id;
                    existing.ManufactureYear = monitor.ManufactureYear;
                }
            }

            // ── USB Devices (only those with serial numbers) ──
            foreach (var usb in report.UsbDevices.Where(u => !string.IsNullOrEmpty(u.SerialNumber)))
            {
                // Find-or-create USB product
                var product = await db.UsbProducts
                    .FirstOrDefaultAsync(p => p.VendorId == usb.VendorId && p.ProductId == usb.ProductId)
                    ?? db.UsbProducts.Local.FirstOrDefault(p => p.VendorId == usb.VendorId && p.ProductId == usb.ProductId);
                if (product is null)
                {
                    product = new UsbProductEntity
                    {
                        Id = Guid.NewGuid(),
                        VendorId = usb.VendorId,
                        ProductId = usb.ProductId,
                        Name = usb.Name,
                        Manufacturer = usb.Manufacturer
                    };
                    db.UsbProducts.Add(product);
                }
                else
                {
                    product.Name = usb.Name;
                    product.Manufacturer = usb.Manufacturer;
                }

                // Find existing asset by serial number, or create new
                var asset = await db.Assets.FirstOrDefaultAsync(a =>
                    a.SerialNumber == usb.SerialNumber && a.Type == nameof(AssetType.UsbPeripheral));

                UsbPeripheralEntity? existing = null;
                if (asset != null)
                    existing = await db.UsbPeripherals.FirstOrDefaultAsync(u => u.Id == asset.Id);

                if (asset is null)
                {
                    asset = new AssetRecord
                    {
                        Id = Guid.NewGuid(),
                        Name = usb.Name,
                        Type = nameof(AssetType.UsbPeripheral),
                        Status = nameof(AssetStatus.InUse),
                        SerialNumber = usb.SerialNumber,
                        Source = nameof(DiscoverySource.Agent),
                        DiscoveredByAgent = report.HardwareUuid,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    };
                    db.Assets.Add(asset);
                }
                else
                {
                    asset.Name = usb.Name;
                    asset.DiscoveredByAgent = report.HardwareUuid;
                    asset.UpdatedAtUtc = now;
                }

                if (existing is null)
                {
                    db.UsbPeripherals.Add(new UsbPeripheralEntity
                    {
                        Id = asset.Id,
                        UsbProductId = product.Id
                    });
                }
                else
                {
                    existing.UsbProductId = product.Id;
                }
            }

            // ── Network Printers ──
            foreach (var printer in report.Printers)
            {
                // Find-or-create printer model (only if we have manufacturer/model info)
                PrinterModelEntity? model = null;
                if (!string.IsNullOrWhiteSpace(printer.Manufacturer) || !string.IsNullOrWhiteSpace(printer.Model))
                {
                    model = await db.PrinterModels
                        .FirstOrDefaultAsync(p => p.Manufacturer == printer.Manufacturer && p.Model == printer.Model)
                        ?? db.PrinterModels.Local.FirstOrDefault(p => p.Manufacturer == printer.Manufacturer && p.Model == printer.Model);
                    if (model is null)
                    {
                        model = new PrinterModelEntity
                        {
                            Id = Guid.NewGuid(),
                            Manufacturer = printer.Manufacturer,
                            Model = printer.Model
                        };
                        db.PrinterModels.Add(model);
                    }
                }

                // Find existing by IP address first, then serial number
                var existing = await db.NetworkPrinters
                    .Include(p => p.Asset)
                    .FirstOrDefaultAsync(p => p.IpAddress == printer.IpAddress);

                if (existing is null && !string.IsNullOrEmpty(printer.SerialNumber))
                {
                    var assetBySerial = await db.Assets.FirstOrDefaultAsync(a =>
                        a.SerialNumber == printer.SerialNumber && a.Type == nameof(AssetType.NetworkPrinter));
                    if (assetBySerial != null)
                        existing = await db.NetworkPrinters.Include(p => p.Asset)
                            .FirstOrDefaultAsync(p => p.Id == assetBySerial.Id);
                }

                var displayName = BuildPrinterName(printer);

                if (existing is null)
                {
                    var asset = new AssetRecord
                    {
                        Id = Guid.NewGuid(),
                        Name = displayName,
                        Type = nameof(AssetType.NetworkPrinter),
                        Status = nameof(AssetStatus.InUse),
                        SerialNumber = printer.SerialNumber,
                        Source = nameof(DiscoverySource.Agent),
                        DiscoveredByAgent = report.HardwareUuid,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    };
                    db.Assets.Add(asset);

                    db.NetworkPrinters.Add(new NetworkPrinterEntity
                    {
                        Id = asset.Id,
                        PrinterModelId = model?.Id,
                        IpAddress = printer.IpAddress,
                        MacAddress = printer.MacAddress,
                        FirmwareVersion = printer.FirmwareVersion,
                        PageCount = printer.PageCount,
                        TonerBlackPercent = printer.TonerBlackPercent,
                        TonerCyanPercent = printer.TonerCyanPercent,
                        TonerMagentaPercent = printer.TonerMagentaPercent,
                        TonerYellowPercent = printer.TonerYellowPercent,
                        Status = printer.Status
                    });
                }
                else
                {
                    if (model != null) existing.PrinterModelId = model.Id;
                    existing.IpAddress = printer.IpAddress;
                    existing.MacAddress = printer.MacAddress ?? existing.MacAddress;
                    existing.FirmwareVersion = printer.FirmwareVersion ?? existing.FirmwareVersion;
                    existing.PageCount = printer.PageCount ?? existing.PageCount;
                    existing.TonerBlackPercent = printer.TonerBlackPercent ?? existing.TonerBlackPercent;
                    existing.TonerCyanPercent = printer.TonerCyanPercent ?? existing.TonerCyanPercent;
                    existing.TonerMagentaPercent = printer.TonerMagentaPercent ?? existing.TonerMagentaPercent;
                    existing.TonerYellowPercent = printer.TonerYellowPercent ?? existing.TonerYellowPercent;
                    existing.Status = printer.Status ?? existing.Status;
                    // Preserve user-edited fields; update agent-sourced fields
                    existing.Asset.Name = displayName;
                    existing.Asset.SerialNumber = printer.SerialNumber ?? existing.Asset.SerialNumber;
                    existing.Asset.DiscoveredByAgent = report.HardwareUuid;
                    existing.Asset.UpdatedAtUtc = now;
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok();
        }).WithName("inventory/peripherals");
    }

    private static string BuildPrinterName(NetworkPrinterInfo printer)
    {
        var parts = new[] { printer.Manufacturer, printer.Model }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var name = string.Join(" ", parts);
        return string.IsNullOrWhiteSpace(name) ? $"Printer ({printer.IpAddress})" : name;
    }
}
