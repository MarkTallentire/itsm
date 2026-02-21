namespace Itsm.Api;

public class AgentRecord
{
    public string HardwareUuid { get; set; } = "";
    public string ComputerName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string AgentVersion { get; set; } = "";
    public bool IsConnected { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public DateTime FirstSeenUtc { get; set; }
}
