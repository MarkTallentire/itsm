using Itsm.Common.Models;

namespace Itsm.Api.Tests;

public static class TestFixtures
{
    public static Computer CreateTestComputer(
        string name = "test-pc",
        string uuid = "test-uuid-1234",
        string serialNumber = "SN-001",
        List<GpuInfo>? gpus = null,
        List<InstalledApp>? apps = null,
        List<DiskInfo>? disks = null)
    {
        return new Computer(
            Identity: new MachineIdentity(name, "MacBook Pro 16", serialNumber, uuid, "testuser", ChassisType.Laptop),
            Cpu: new CpuInfo("Apple M2 Pro", 12, "arm64"),
            Memory: new MemoryInfo(34359738368),
            Disks: disks ?? [new DiskInfo("Macintosh HD", "APFS", 1000000000000, 500000000000)],
            Os: new OsInfo("macOS 15.2", "15.2", "24C101"),
            Network: new NetworkInfo("test-pc.local", [
                new NetworkInterfaceInfo("en0", "AA:BB:CC:DD:EE:FF", ["192.168.1.100", "fe80::1"])
            ]),
            Gpus: gpus ?? [new GpuInfo("Apple M2 Pro GPU", "Apple", 0, null)],
            Battery: new BatteryInfo(true, 85.0, 120, 92.0, false, "Normal"),
            InstalledApps: apps ?? [new InstalledApp("Visual Studio Code", "1.85.0", "2024-01-15")],
            Uptime: new UptimeInfo(DateTime.UtcNow.AddHours(-5), TimeSpan.FromHours(5)),
            Firewall: new FirewallInfo(true, true),
            Encryption: new EncryptionInfo(true, "FileVault")
        );
    }

    public static PeripheralReport CreateTestPeripheralReport(
        string uuid = "test-uuid-1234",
        string computerName = "test-pc",
        List<MonitorInfo>? monitors = null,
        List<UsbDeviceInfo>? usbDevices = null,
        List<NetworkPrinterInfo>? printers = null)
    {
        return new PeripheralReport(
            HardwareUuid: uuid,
            ComputerName: computerName,
            Monitors: monitors ?? [
                new MonitorInfo("Dell", "U2723QE", "SN-MON-001", 2023, 3840, 2160, 27.0)
            ],
            UsbDevices: usbDevices ?? [
                new UsbDeviceInfo("046d", "c52b", "Logitech Unifying Receiver", "Logitech", "SN-USB-001")
            ],
            Printers: printers ?? [
                new NetworkPrinterInfo("192.168.1.200", "00:11:22:33:44:55", "HP", "LaserJet Pro M404n",
                    "SN-PRT-001", "FW-2.73", 12345, 80, 60, 45, 70, "Online")
            ]
        );
    }
}
