namespace Itsm.Common.Models;

public record NetworkInfo(string Hostname, List<NetworkInterfaceInfo> Interfaces);
