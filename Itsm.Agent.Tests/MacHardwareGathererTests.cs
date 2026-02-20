using Itsm.Agent;
using Itsm.Common;
using Itsm.Common.Models;
using Xunit;

namespace Itsm.Agent.Tests;

public class MacHardwareGathererTests
{
    private readonly FakeCommandRunner _commandRunner = new();
    private readonly MacHardwareGatherer _gatherer;

    public MacHardwareGathererTests()
    {
        _gatherer = new MacHardwareGatherer(_commandRunner);
    }

    [Fact]
    public void GetMachineIdentity_ParsesSystemProfilerOutput()
    {
        _commandRunner.Setup("system_profiler", "SPHardwareDataType", """
            Hardware:

                Hardware Overview:

                  Model Name: MacBook Pro
                  Model Identifier: Mac14,10
                  Model Number: Z17G000NAB/A
                  Chip: Apple M2 Pro
                  Total Number of Cores: 12
                  Memory: 32 GB
                  System Firmware Version: 10151.140.19
                  OS Loader Version: 10151.140.19
                  Serial Number (system): ABC123XYZ
                  Hardware UUID: 12345678-1234-1234-1234-123456789ABC
                  Provisioning UDID: 00006020-000000000000000E
                  Activation Lock Status: Enabled
            """);
        _commandRunner.Setup("scutil", "--get ComputerName", "Marks-MacBook-Pro");

        var identity = _gatherer.GetMachineIdentity();

        Assert.Equal("Marks-MacBook-Pro", identity.ComputerName);
        Assert.Equal("MacBook Pro", identity.ModelName);
        Assert.Equal("ABC123XYZ", identity.SerialNumber);
        Assert.Equal("12345678-1234-1234-1234-123456789ABC", identity.HardwareUuid);
        Assert.Equal(Environment.UserName, identity.LoggedInUser);
        Assert.Equal(ChassisType.Laptop, identity.ChassisType);
    }

    [Fact]
    public void GetMachineIdentity_ReturnsUnknown_WhenFieldsMissing()
    {
        _commandRunner.Setup("system_profiler", "SPHardwareDataType", """
            Hardware:

                Hardware Overview:

                  Chip: Apple M2 Pro
            """);
        _commandRunner.Setup("scutil", "--get ComputerName", "Test-Mac");

        var identity = _gatherer.GetMachineIdentity();

        Assert.Equal("Test-Mac", identity.ComputerName);
        Assert.Equal("Unknown", identity.ModelName);
        Assert.Equal("Unknown", identity.SerialNumber);
        Assert.Equal("Unknown", identity.HardwareUuid);
        Assert.Equal(ChassisType.Unknown, identity.ChassisType);
    }

    [Fact]
    public void GetCpuInformation_ParsesBrandString()
    {
        _commandRunner.Setup("sysctl", "-n machdep.cpu.brand_string", "Apple M2 Pro");

        var cpu = _gatherer.GetCpuInformation();

        Assert.Equal("Apple M2 Pro", cpu.BrandString);
        Assert.True(cpu.CoreCount > 0);
        Assert.False(string.IsNullOrEmpty(cpu.Architecture));
    }

    [Fact]
    public void GetMachineIdentity_ParsesUuidWithColons()
    {
        _commandRunner.Setup("system_profiler", "SPHardwareDataType", """
            Hardware:

                Hardware Overview:

                  Hardware UUID: AABB:CCDD-1234-5678-9ABC-DEF012345678
            """);
        _commandRunner.Setup("scutil", "--get ComputerName", "Test-Mac");

        var identity = _gatherer.GetMachineIdentity();

        Assert.Equal("AABB:CCDD-1234-5678-9ABC-DEF012345678", identity.HardwareUuid);
    }

    [Theory]
    [InlineData("MacBook Pro", ChassisType.Laptop)]
    [InlineData("MacBook Air", ChassisType.Laptop)]
    [InlineData("iMac", ChassisType.AllInOne)]
    [InlineData("Mac mini", ChassisType.Mini)]
    [InlineData("Mac Pro", ChassisType.Tower)]
    [InlineData("Mac Studio", ChassisType.Desktop)]
    [InlineData("SomeFutureMac", ChassisType.Unknown)]
    public void GetMachineIdentity_ClassifiesChassisType(string modelName, ChassisType expected)
    {
        _commandRunner.Setup("system_profiler", "SPHardwareDataType", $"""
            Hardware:

                Hardware Overview:

                  Model Name: {modelName}
            """);
        _commandRunner.Setup("scutil", "--get ComputerName", "Test-Mac");

        var identity = _gatherer.GetMachineIdentity();

        Assert.Equal(expected, identity.ChassisType);
    }
}
