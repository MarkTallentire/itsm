namespace Itsm.Common.Models;

public record BatteryInfo(bool IsPresent, double? ChargePercent, int? CycleCount, double? HealthPercent, bool? IsCharging, string? Condition);
