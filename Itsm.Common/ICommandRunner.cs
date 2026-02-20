namespace Itsm.Common;

public interface ICommandRunner
{
    string Run(string fileName, string arguments);
}
