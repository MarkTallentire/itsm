namespace Itsm.Common.Models;

public record NetworkInfo(
    string Hostname,
    List<NetworkInterfaceInfo> Interfaces,
    List<VpnConnection> VpnConnections,
    DnsConfiguration Dns,
    List<NetworkDrive> NetworkDrives,
    List<ListeningPort> ListeningPorts);

public record VpnConnection(string Name, string? Type, string? ServerAddress, bool IsConnected);

public record DnsConfiguration(List<string> Servers, string? Domain, List<string> SearchDomains);

public record NetworkDrive(string LocalPath, string RemotePath, string? FileSystem);

public record ListeningPort(int Port, string Protocol, string? ProcessName, int? Pid);
