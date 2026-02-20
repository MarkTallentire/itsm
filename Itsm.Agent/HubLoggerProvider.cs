using System.Collections.Concurrent;
using Itsm.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Itsm.Agent;

public sealed class HubLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, HubLogger> _loggers = new();
    private HubConnection? _connection;

    public void SetConnection(HubConnection connection) => _connection = connection;

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new HubLogger(name, this));

    internal void SendLog(LogEntry entry)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            // Fire-and-forget — don't block the logging call
            _ = _connection.SendAsync("SendLog", entry);
        }
    }

    public void Dispose() => _loggers.Clear();

    private sealed class HubLogger(string category, HubLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            if (exception is not null)
                message += $"\n{exception}";

            provider.SendLog(new LogEntry(
                DateTime.UtcNow,
                logLevel.ToString(),
                category,
                message));
        }
    }
}
