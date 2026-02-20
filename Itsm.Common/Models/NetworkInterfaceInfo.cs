namespace Itsm.Common.Models;

public record NetworkInterfaceInfo(string Name, string MacAddress, List<string> IpAddresses);
