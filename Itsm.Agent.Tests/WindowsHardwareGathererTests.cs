using Itsm.Agent;
using Itsm.Common;
using Itsm.Common.Models;
using Xunit;

namespace Itsm.Agent.Tests;

public class WindowsHardwareGathererTests
{
    private readonly FakeCommandRunner _commandRunner = new();
    private readonly WindowsHardwareGatherer _gatherer;

    public WindowsHardwareGathererTests()
    {
        _gatherer = new WindowsHardwareGatherer(_commandRunner);
    }

    [Fact]
    public void GetCpuInformation_ParsesWmicOutput()
    {
        _commandRunner.Setup("wmic", "cpu get Name /format:value", "\r\nName=Intel(R) Core(TM) i7-10750H CPU @ 2.60GHz\r\n");

        var cpu = _gatherer.GetCpuInformation();

        Assert.Equal("Intel(R) Core(TM) i7-10750H CPU @ 2.60GHz", cpu.BrandString);
    }

    [Fact]
    public void GetMachineIdentity_ParsesAllWmicFields()
    {
        _commandRunner.Setup("hostname", "", "DESKTOP-ABC123");
        _commandRunner.Setup("wmic", "computersystem get Model /format:value", "\r\nModel=ThinkPad X1 Carbon Gen 9\r\n");
        _commandRunner.Setup("wmic", "bios get SerialNumber /format:value", "\r\nSerialNumber=PF2XXXXX\r\n");
        _commandRunner.Setup("wmic", "csproduct get UUID /format:value", "\r\nUUID=12345678-1234-1234-1234-123456789ABC\r\n");
        _commandRunner.Setup("wmic", "systemenclosure get ChassisTypes /format:value", "\r\nChassisTypes={9}\r\n");

        var identity = _gatherer.GetMachineIdentity();

        Assert.Equal("DESKTOP-ABC123", identity.ComputerName);
        Assert.Equal("ThinkPad X1 Carbon Gen 9", identity.ModelName);
        Assert.Equal("PF2XXXXX", identity.SerialNumber);
        Assert.Equal("12345678-1234-1234-1234-123456789ABC", identity.HardwareUuid);
        Assert.Equal(ChassisType.Laptop, identity.ChassisType);
    }

    [Theory]
    [InlineData("{3}", ChassisType.Desktop)]
    [InlineData("{7}", ChassisType.Tower)]
    [InlineData("{9}", ChassisType.Laptop)]
    [InlineData("{10}", ChassisType.Laptop)]
    [InlineData("{13}", ChassisType.AllInOne)]
    [InlineData("{30}", ChassisType.Tablet)]
    [InlineData("{34}", ChassisType.Mini)]
    [InlineData("{1}", ChassisType.Unknown)]
    public void ParseChassisType_MapsWmiCodes(string wmicValue, ChassisType expected)
    {
        var output = $"\r\nChassisTypes={wmicValue}\r\n";
        Assert.Equal(expected, WindowsHardwareGatherer.ParseChassisType(output));
    }

    [Fact]
    public void GetMachineIdentity_ReturnsUnknown_WhenFieldsMissing()
    {
        _commandRunner.Setup("hostname", "", "DESKTOP-ABC123");
        _commandRunner.Setup("wmic", "computersystem get Model /format:value", "");
        _commandRunner.Setup("wmic", "bios get SerialNumber /format:value", "");
        _commandRunner.Setup("wmic", "csproduct get UUID /format:value", "");
        _commandRunner.Setup("wmic", "systemenclosure get ChassisTypes /format:value", "");

        var identity = _gatherer.GetMachineIdentity();

        Assert.Equal("Unknown", identity.ModelName);
        Assert.Equal("Unknown", identity.SerialNumber);
        Assert.Equal("Unknown", identity.HardwareUuid);
        Assert.Equal(ChassisType.Unknown, identity.ChassisType);
    }
}
