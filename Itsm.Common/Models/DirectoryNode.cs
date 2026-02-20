namespace Itsm.Common.Models;

public record DirectoryNode(string Path, long SizeBytes, List<DirectoryNode> Children);
