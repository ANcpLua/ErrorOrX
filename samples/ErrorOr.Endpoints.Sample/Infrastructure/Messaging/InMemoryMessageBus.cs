using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace ErrorOr.Endpoints.Sample.Infrastructure.Messaging;

internal sealed class InMemoryMessagePublisher : IMessagePublisher
{
    private readonly InMemoryMessageBus _bus;

    public InMemoryMessagePublisher(InMemoryMessageBus bus)
    {
        _bus = bus;
    }

    public Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default) where T : class
    {
        _bus.Publish(routingKey, message);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryMessageBus
{
    private readonly ConcurrentDictionary<QueueKey, object> _queues = new();

    public void Publish<T>(string routingKey, T message) where T : class
    {
        var channel = (Channel<T>)_queues.GetOrAdd(
            new QueueKey(routingKey, typeof(T)),
            static _ => Channel.CreateUnbounded<T>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            }));

        channel.Writer.TryWrite(message);
    }

    public ChannelReader<T> GetQueue<T>(string routingKey) where T : class
    {
        var channel = (Channel<T>)_queues.GetOrAdd(
            new QueueKey(routingKey, typeof(T)),
            static _ => Channel.CreateUnbounded<T>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            }));

        return channel.Reader;
    }

    private readonly record struct QueueKey(string RoutingKey, Type MessageType);
}

internal sealed class InMemoryConsumerFactory : IMessageConsumerFactory
{
    private readonly InMemoryMessageBus _bus;

    public InMemoryConsumerFactory(InMemoryMessageBus bus)
    {
        _bus = bus;
    }

    public Task<IMessageConsumer<T>> CreateAsync<T>() where T : class
    {
        var queueName = typeof(T).Name;
        return Task.FromResult<IMessageConsumer<T>>(new InMemoryConsumer<T>(_bus.GetQueue<T>(queueName)));
    }
}

file sealed class InMemoryConsumer<T> : IMessageConsumer<T> where T : class
{
    private readonly ChannelReader<T> _reader;

    public InMemoryConsumer(ChannelReader<T> reader)
    {
        _reader = reader;
    }

    public async IAsyncEnumerable<T> ConsumeAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var item in _reader.ReadAllAsync(ct))
            yield return item;
    }

    public Task AckAsync()
    {
        return Task.CompletedTask;
    }

    public Task NackAsync(bool requeue = true)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}