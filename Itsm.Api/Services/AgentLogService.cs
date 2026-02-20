using System.Collections.Concurrent;
using System.Threading.Channels;
using Itsm.Common.Models;

namespace Itsm.Api.Services;

public class AgentLogService
{
    private const int MaxBufferSize = 500;

    private readonly ConcurrentDictionary<string, BoundedBuffer> _buffers = new();
    private readonly ConcurrentDictionary<string, List<Channel<LogEntry>>> _subscribers = new();
    private readonly Lock _subscriberLock = new();

    public void AddLog(string hardwareUuid, LogEntry entry)
    {
        var buffer = _buffers.GetOrAdd(hardwareUuid, _ => new BoundedBuffer(MaxBufferSize));
        buffer.Add(entry);

        if (_subscribers.TryGetValue(hardwareUuid, out var channels))
        {
            lock (_subscriberLock)
            {
                foreach (var channel in channels)
                {
                    channel.Writer.TryWrite(entry);
                }
            }
        }
    }

    public IReadOnlyList<LogEntry> GetRecentLogs(string hardwareUuid)
    {
        return _buffers.TryGetValue(hardwareUuid, out var buffer)
            ? buffer.GetAll()
            : [];
    }

    public (Channel<LogEntry> Channel, IDisposable Subscription) Subscribe(string hardwareUuid)
    {
        var channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        var channels = _subscribers.GetOrAdd(hardwareUuid, _ => []);
        lock (_subscriberLock)
        {
            channels.Add(channel);
        }

        return (channel, new Unsubscriber(() =>
        {
            lock (_subscriberLock)
            {
                channels.Remove(channel);
            }
            channel.Writer.TryComplete();
        }));
    }

    private sealed class Unsubscriber(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }

    private sealed class BoundedBuffer(int maxSize)
    {
        private readonly Lock _lock = new();
        private readonly LinkedList<LogEntry> _entries = new();

        public void Add(LogEntry entry)
        {
            lock (_lock)
            {
                _entries.AddLast(entry);
                while (_entries.Count > maxSize)
                    _entries.RemoveFirst();
            }
        }

        public IReadOnlyList<LogEntry> GetAll()
        {
            lock (_lock)
            {
                return [.. _entries];
            }
        }
    }
}
