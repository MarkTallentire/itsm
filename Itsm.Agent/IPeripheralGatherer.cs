using Itsm.Common.Models;

namespace Itsm.Agent;

public interface IPeripheralGatherer
{
    List<MonitorInfo> GetMonitors();
    List<UsbDeviceInfo> GetUsbDevices();
}
