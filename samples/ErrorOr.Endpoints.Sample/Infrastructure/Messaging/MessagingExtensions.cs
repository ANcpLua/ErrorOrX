using System.Diagnostics.CodeAnalysis;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ErrorOr.Endpoints.Sample.Infrastructure.Messaging;

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

    /// <summary>
    ///     Maps an SSE endpoint for string events (always AOT-safe).
    /// </summary>
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

            // String serialization doesn't require JSON, always AOT-safe
            return TypedResults.ServerSentEvents(reader.ReadAllAsync(context.RequestAborted), eventType);
        }).ExcludeFromDescription();
    }

    /// <summary>
    ///     Maps an SSE endpoint with explicit JsonTypeInfo for 100% AOT safety.
    ///     This is the recommended overload for Native AOT deployments.
    /// </summary>
    /// <typeparam name="T">The type of events to stream</typeparam>
    /// <param name="endpoints">The endpoint route builder</param>
    /// <param name="pattern">The URL pattern for the endpoint</param>
    /// <param name="eventType">The SSE event type</param>
    /// <param name="jsonTypeInfo">The source-generated JsonTypeInfo for T</param>
    [RequiresDynamicCode("Use the overload with JsonTypeInfo<T> for AOT safety")]
    [RequiresUnreferencedCode("Use the overload with JsonTypeInfo<T> for AOT safety")]
    public static RouteHandlerBuilder MapSse<T>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string eventType,
        JsonTypeInfo<T> jsonTypeInfo) where T : class
    {
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        return endpoints.MapGet(pattern, (ISseStream<T> stream, HttpContext context) =>
        {
            var clientId = Guid.NewGuid();
            var reader = stream.Subscribe(clientId);
            context.RequestAborted.Register(() => stream.Unsubscribe(clientId));

            // Use explicit JsonTypeInfo for AOT-safe serialization
            return TypedResults.ServerSentEvents(reader.ReadAllAsync(context.RequestAborted), eventType);
        }).ExcludeFromDescription();
    }

    /// <summary>
    ///     Maps an SSE endpoint. AOT-safe when JsonSerializerContext is configured globally.
    /// </summary>
    /// <typeparam name="T">The type of events to stream</typeparam>
    /// <param name="endpoints">The endpoint route builder</param>
    /// <param name="pattern">The URL pattern for the endpoint</param>
    /// <param name="eventType">The SSE event type</param>
    /// <remarks>
    ///     For AOT safety, ensure JsonSerializerContext is configured globally via
    ///     services.AddErrorOrEndpointJson&lt;AppJsonSerializerContext&gt;() and that T
    ///     is registered with [JsonSerializable(typeof(T))] attribute.
    /// </remarks>
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

            return TypedResults.ServerSentEvents(reader.ReadAllAsync(context.RequestAborted), eventType);
        }).ExcludeFromDescription();
    }

    /// <summary>
    ///     Maps an SSE endpoint with explicit JsonSerializerContext for compile-time AOT verification.
    ///     Use this when you want the compiler to verify your type is registered in the context.
    /// </summary>
    /// <typeparam name="T">The type of events to stream</typeparam>
    /// <typeparam name="TContext">The JsonSerializerContext that contains JsonTypeInfo for T</typeparam>
    public static RouteHandlerBuilder MapSseAot<T, TContext>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string eventType)
        where T : class
        where TContext : JsonSerializerContext, new()
    {
        // This verifies at startup that the type is registered
        var context = new TContext();
        _ = context.GetTypeInfo(typeof(T))
            ?? throw new InvalidOperationException(
                $"Type '{typeof(T).Name}' is not registered in '{typeof(TContext).Name}'. " +
                $"Add [JsonSerializable(typeof({typeof(T).Name}))] to your JsonSerializerContext.");

        return endpoints.MapGet(pattern, (ISseStream<T> stream, HttpContext httpContext) =>
        {
            var clientId = Guid.NewGuid();
            var reader = stream.Subscribe(clientId);
            httpContext.RequestAborted.Register(() => stream.Unsubscribe(clientId));

            return TypedResults.ServerSentEvents(reader.ReadAllAsync(httpContext.RequestAborted), eventType);
        }).ExcludeFromDescription();
    }

    /// <summary>
    ///     Maps an SSE endpoint for pre-formed SseItem events.
    ///     AOT-safe when JsonSerializerContext is configured globally.
    /// </summary>
    public static RouteHandlerBuilder MapSseItems<T>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where T : class
    {
        return endpoints.MapGet(pattern, static (ISseStream<SseItem<T>> stream, HttpContext context) =>
        {
            var clientId = Guid.NewGuid();
            var reader = stream.Subscribe(clientId);
            context.RequestAborted.Register(() => stream.Unsubscribe(clientId));

            return TypedResults.ServerSentEvents(reader.ReadAllAsync(context.RequestAborted));
        }).ExcludeFromDescription();
    }

    /// <summary>
    ///     Maps an SSE endpoint with custom payload projection.
    ///     AOT-safe when JsonSerializerContext is configured globally and
    ///     includes the TPayload type.
    /// </summary>
    /// <typeparam name="TIn">The type of input events from the stream</typeparam>
    /// <typeparam name="TPayload">The type of payload to send to clients</typeparam>
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