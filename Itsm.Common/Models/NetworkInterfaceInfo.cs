namespace Itsm.Common.Models;

public record NetworkInterfaceInfo(
    string Name,
    string MacAddress,
    List<string> IpAddresses,
    long? SpeedMbps,
    string? InterfaceType,
    bool? IsDhcp,
    string? Gateway,
    string? SubnetMask,
    string? WifiSsid,
    double? WifiFrequencyGHz,
    int? WifiSignalDbm);
