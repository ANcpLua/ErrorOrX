using System;
using System.Collections.Generic;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ErrorOr.Http.Bcl.Sample.Infrastructure.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryMessageBus>();
        services.AddSingleton<IMessagePublisher, InMemoryMessagePublisher>();
        services.AddSingleton<IMessageConsumerFactory, InMemoryConsumerFactory>();
        return services;
    }

    public static IServiceCollection AddNullMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IMessagePublisher>(NullMessagePublisher.Instance);
        services.AddSingleton<IMessageConsumerFactory>(NullConsumerFactory.Instance);
        return services;
    }

    public static IServiceCollection AddSseStream<T>(this IServiceCollection services) where T : class
    {
        services.AddSingleton<ISseStream<T>, SseStream<T>>();
        return services;
    }

    public static IServiceCollection AddNullSseStream<T>(this IServiceCollection services) where T : class
    {
        services.AddSingleton<ISseStream<T>, NullSseStream<T>>();
        return services;
    }

    public static IServiceCollection AddSseItemStream<T>(this IServiceCollection services) where T : class
    {
        services.AddSingleton<ISseStream<SseItem<T>>, SseStream<SseItem<T>>>();
        return services;
    }

    public static IServiceCollection AddNullSseItemStream<T>(this IServiceCollection services) where T : class
    {
        services.AddSingleton<ISseStream<SseItem<T>>, NullSseStream<SseItem<T>>>();
        return services;
    }

    public static RouteHandlerBuilder MapSseStrings(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string eventType)
    {
        return endpoints.MapGet(pattern, (ISseStream<string> stream, HttpContext context) =>
        {
            var clientId = Guid.NewGuid();
            var reader = stream.Subscribe(clientId);
            context.RequestAborted.Register(() => stream.Unsubscribe(clientId));

            return TypedResults.ServerSentEvents(reader.ReadAllAsync(context.RequestAborted), eventType: eventType);
        }).ExcludeFromDescription();
    }

    public static RouteHandlerBuilder MapSse<T>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string eventType) where T : class
    {
        return endpoints.MapGet(pattern, (ISseStream<T> stream, HttpContext context) =>
        {
            var clientId = Guid.NewGuid();
            var reader = stream.Subscribe(clientId);
            context.RequestAborted.Register(() => stream.Unsubscribe(clientId));

            return TypedResults.ServerSentEvents(reader.ReadAllAsync(context.RequestAborted), eventType: eventType);
        }).ExcludeFromDescription();
    }

    public static RouteHandlerBuilder MapSseItems<T>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where T : class
    {
        return endpoints.MapGet(pattern, (ISseStream<SseItem<T>> stream, HttpContext context) =>
        {
            var clientId = Guid.NewGuid();
            var reader = stream.Subscribe(clientId);
            context.RequestAborted.Register(() => stream.Unsubscribe(clientId));

            return TypedResults.ServerSentEvents(reader.ReadAllAsync(context.RequestAborted));
        }).ExcludeFromDescription();
    }

    public static RouteHandlerBuilder MapSseItems<TIn, TPayload>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<TIn, TPayload> payloadSelector,
        Func<TIn, string?> eventTypeSelector,
        Func<TIn, string?>? eventIdSelector = null,
        Func<TIn, TimeSpan?>? reconnectionIntervalSelector = null)
        where TIn : class
    {
        return endpoints.MapGet(pattern, (ISseStream<TIn> stream, HttpContext context) =>
        {
            var clientId = Guid.NewGuid();
            var reader = stream.Subscribe(clientId);
            context.RequestAborted.Register(() => stream.Unsubscribe(clientId));

            return TypedResults.ServerSentEvents(Project(context.RequestAborted));

            async IAsyncEnumerable<SseItem<TPayload>> Project([EnumeratorCancellation] CancellationToken ct)
            {
                await foreach (var item in reader.ReadAllAsync(ct))
                {
                    var payload = payloadSelector(item);
                    var eventType = eventTypeSelector(item);
                    var sse = new SseItem<TPayload>(payload, eventType);

                    var eventId = eventIdSelector?.Invoke(item);
                    if (eventId is not null)
                        sse = sse with { EventId = eventId };

                    var retry = reconnectionIntervalSelector?.Invoke(item);
                    if (retry is not null)
                        sse = sse with { ReconnectionInterval = retry };

                    yield return sse;
                }
            }
        }).ExcludeFromDescription();
    }
}