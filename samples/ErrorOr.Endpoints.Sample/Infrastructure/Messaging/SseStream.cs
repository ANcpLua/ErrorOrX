using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ErrorOr.Endpoints.Sample.Infrastructure.Messaging;

internal sealed class SseStream<T> : ISseStream<T>
{
    private readonly ConcurrentDictionary<Guid, Channel<T>> _channels = new();
    private volatile bool _disposed;

    public int ClientCount => _channels.Count;

    public ChannelReader<T> Subscribe(Guid clientId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var channel = Channel.CreateBounded<T>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = false
        });

        _channels[clientId] = channel;
        return channel.Reader;
    }

    public void Unsubscribe(Guid clientId)
    {
        if (_channels.TryRemove(clientId, out var channel))
            channel.Writer.TryComplete();
    }

    public void Publish(T item)
    {
        if (_disposed) return;

        foreach (var channel in _channels.Values)
            channel.Writer.TryWrite(item);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        _disposed = true;

        foreach (var channel in _channels.Values)
            channel.Writer.TryComplete();

        _channels.Clear();
        return ValueTask.CompletedTask;
    }
}