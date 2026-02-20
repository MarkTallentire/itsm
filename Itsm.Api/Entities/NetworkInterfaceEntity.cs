namespace Itsm.Api;

public class NetworkInterfaceEntity
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public string Name { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string[] IpAddresses { get; set; } = [];
    public long? SpeedMbps { get; set; }
    public string? InterfaceType { get; set; }
    public bool? IsDhcp { get; set; }
    public string? Gateway { get; set; }
    public string? SubnetMask { get; set; }
    public string? WifiSsid { get; set; }
    public double? WifiFrequencyGHz { get; set; }
    public int? WifiSignalDbm { get; set; }

    public ComputerEntity Computer { get; set; } = null!;
}
