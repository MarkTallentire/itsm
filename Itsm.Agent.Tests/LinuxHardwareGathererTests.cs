using Itsm.Agent;
using Itsm.Common;
using Itsm.Common.Models;
using Xunit;

namespace Itsm.Agent.Tests;

public class LinuxHardwareGathererTests
{
    private readonly FakeCommandRunner _commandRunner = new();
    private readonly LinuxHardwareGatherer _gatherer;

    public LinuxHardwareGathererTests()
    {
        _gatherer = new LinuxHardwareGatherer(_commandRunner);
    }

    [Fact]
    public void GetCpuInformation_ParsesProcCpuinfo()
    {
        _commandRunner.Setup("cat", "/proc/cpuinfo", """
            processor	: 0
            vendor_id	: GenuineIntel
            cpu family	: 6
            model		: 142
            model name	: Intel(R) Core(TM) i7-8550U CPU @ 1.80GHz
            stepping	: 10
            cpu MHz		: 2000.000
            """);

        var cpu = _gatherer.GetCpuInformation();

        Assert.Equal("Intel(R) Core(TM) i7-8550U CPU @ 1.80GHz", cpu.BrandString);
    }

    [Fact]
    public void GetMachineIdentity_ParsesDmiFiles()
    {
        _commandRunner.Setup("hostname", "", "ubuntu-server");
        _commandRunner.Setup("cat", "/sys/class/dmi/id/product_name", "ThinkPad T480s");
        _commandRunner.Setup("cat", "/sys/class/dmi/id/product_serial", "PF1ABCDE");
        _commandRunner.Setup("cat", "/sys/class/dmi/id/product_uuid", "abcdef01-2345-6789-abcd-ef0123456789");
        _commandRunner.Setup("cat", "/sys/class/dmi/id/chassis_type", "10");

        var identity = _gatherer.GetMachineIdentity();

        Assert.Equal("ubuntu-server", identity.ComputerName);
        Assert.Equal("ThinkPad T480s", identity.ModelName);
        Assert.Equal("PF1ABCDE", identity.SerialNumber);
        Assert.Equal("abcdef01-2345-6789-abcd-ef0123456789", identity.HardwareUuid);
        Assert.Equal(ChassisType.Laptop, identity.ChassisType);
    }

    [Theory]
    [InlineData("3", ChassisType.Desktop)]
    [InlineData("7", ChassisType.Tower)]
    [InlineData("9", ChassisType.Laptop)]
    [InlineData("10", ChassisType.Laptop)]
    [InlineData("13", ChassisType.AllInOne)]
    [InlineData("30", ChassisType.Tablet)]
    [InlineData("34", ChassisType.Mini)]
    [InlineData("1", ChassisType.Unknown)]
    [InlineData("garbage", ChassisType.Unknown)]
    public void ParseChassisType_MapsSmbiosCodes(string code, ChassisType expected)
    {
        Assert.Equal(expected, LinuxHardwareGatherer.ParseChassisType(code));
    }

    [Fact]
    public void GetCpuInformation_ReturnsUnknown_WhenModelNameMissing()
    {
        _commandRunner.Setup("cat", "/proc/cpuinfo", """
            processor	: 0
            vendor_id	: GenuineIntel
            """);

        var cpu = _gatherer.GetCpuInformation();

        Assert.Equal("Unknown", cpu.BrandString);
    }
}
