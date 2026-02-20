using System.Diagnostics;
using Itsm.Common;

namespace Itsm.Agent;

public class CommandRunner : ICommandRunner
{
    public string Run(string fileName, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        process.Start();
        var result = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return result;
    }
}
