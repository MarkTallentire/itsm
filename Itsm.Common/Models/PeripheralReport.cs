namespace Itsm.Common.Models;

public record PeripheralReport(
    string HardwareUuid,
    string ComputerName,
    List<MonitorInfo> Monitors,
    List<UsbDeviceInfo> UsbDevices,
    List<NetworkPrinterInfo> Printers);
