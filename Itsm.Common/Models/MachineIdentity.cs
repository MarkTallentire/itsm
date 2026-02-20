namespace Itsm.Common.Models;

public record MachineIdentity(string ComputerName, string ModelName, string SerialNumber, string HardwareUuid, string LoggedInUser, ChassisType ChassisType);
