namespace Itsm.Common.Models;

public record VirtualizationInfo(List<VmInstance> VirtualMachines, List<DockerContainer> DockerContainers);

public record VmInstance(string Name, string? State, string? Type, long? MemoryMB, int? CpuCount);

public record DockerContainer(string Id, string Name, string Image, string State, string? Status);
