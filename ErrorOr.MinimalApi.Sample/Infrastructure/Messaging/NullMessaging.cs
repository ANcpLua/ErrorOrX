using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ErrorOr.Http.Bcl.Sample.Infrastructure.Messaging;

internal sealed class NullMessagePublisher : IMessagePublisher
{
    public static readonly NullMessagePublisher Instance = new();

    public Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default) where T : class
        => Task.CompletedTask;
}

internal sealed class NullConsumerFactory : IMessageConsumerFactory
{
    public static readonly NullConsumerFactory Instance = new();

    public Task<IMessageConsumer<T>> CreateAsync<T>() where T : class
        => Task.FromResult<IMessageConsumer<T>>(NullConsumer<T>.Instance);
}

file sealed class NullConsumer<T> : IMessageConsumer<T> where T : class
{
    public static readonly NullConsumer<T> Instance = new();

    public async IAsyncEnumerable<T> ConsumeAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await Task.Delay(Timeout.Infinite, ct);
        yield break;
    }

    public Task AckAsync() => Task.CompletedTask;
    public Task NackAsync(bool requeue = true) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class NullSseStream<T> : ISseStream<T>
{
    private readonly Channel<T> _empty = Channel.CreateBounded<T>(1);

    public int ClientCount => 0;

    public ChannelReader<T> Subscribe(Guid clientId) => _empty.Reader;

    public void Unsubscribe(Guid clientId) { }

    public void Publish(T item) { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}