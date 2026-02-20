using Itsm.Common;

namespace Itsm.Agent.Tests;

public class FakeCommandRunner : ICommandRunner
{
    private readonly Dictionary<string, string> _responses = new();

    public void Setup(string fileName, string arguments, string output)
    {
        _responses[$"{fileName}|{arguments}"] = output;
    }

    public string Run(string fileName, string arguments)
    {
        var key = $"{fileName}|{arguments}";
        if (_responses.TryGetValue(key, out var result))
            return result;
        throw new InvalidOperationException($"No fake response configured for: {fileName} {arguments}");
    }
}
