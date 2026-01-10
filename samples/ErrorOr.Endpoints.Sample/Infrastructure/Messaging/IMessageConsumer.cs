namespace ErrorOr.Endpoints.Sample.Infrastructure.Messaging;

public interface IMessageConsumer<out T> : IAsyncDisposable where T : class
{
    IAsyncEnumerable<T> ConsumeAsync(CancellationToken ct);
    Task AckAsync();
    Task NackAsync(bool requeue = true);
}

public interface IMessageConsumerFactory
{
    Task<IMessageConsumer<T>> CreateAsync<T>() where T : class;
}