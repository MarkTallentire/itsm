using System.Runtime.InteropServices;
using Itsm.Common;

namespace Itsm.Agent;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddServiceDefaults();
        builder.Services.AddSingleton<ICommandRunner, CommandRunner>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            builder.Services.AddSingleton<IHardwareGatherer, MacHardwareGatherer>();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            builder.Services.AddSingleton<IHardwareGatherer, WindowsHardwareGatherer>();
        else
            builder.Services.AddSingleton<IHardwareGatherer, LinuxHardwareGatherer>();

        builder.Services.AddHttpClient("itsm-api", client =>
        {
            client.BaseAddress = new Uri("https+http://itsm-api");
        });
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}
