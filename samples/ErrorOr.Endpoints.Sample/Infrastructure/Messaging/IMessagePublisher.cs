namespace ErrorOr.Endpoints.Sample.Infrastructure.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default) where T : class;
}