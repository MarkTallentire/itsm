namespace Itsm.Common.Models;

public record LogEntry(DateTime TimestampUtc, string Level, string Category, string Message);
