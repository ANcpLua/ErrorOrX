using System.Threading.Channels;

namespace ErrorOr.Endpoints.Sample.Infrastructure.Messaging;

public interface ISseStream<T> : IAsyncDisposable
{
    int ClientCount { get; }

    ChannelReader<T> Subscribe(Guid clientId);

    void Unsubscribe(Guid clientId);

    void Publish(T item);
}