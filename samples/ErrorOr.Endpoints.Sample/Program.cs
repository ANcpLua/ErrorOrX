using ErrorOr.Endpoints.Generated;
using ErrorOr.Endpoints.Sample;
using ErrorOr.Endpoints.Sample.Domain;
using ErrorOr.Endpoints.Sample.Infrastructure.Messaging;
using AppJsonSerializerContext = ErrorOr.Endpoints.Sample.AppJsonSerializerContext;

var builder = WebApplication.CreateSlimBuilder(args);

// Register custom route constraints for .NET 6+ date/time types (not built-in to ASP.NET Core)
builder.Services.Configure<RouteOptions>(static options =>
{
    options.ConstraintMap["dateonly"] = typeof(DateOnlyRouteConstraint);
    options.ConstraintMap["timeonly"] = typeof(TimeOnlyRouteConstraint);
    options.ConstraintMap["timespan"] = typeof(TimeSpanRouteConstraint);
    options.ConstraintMap["datetimeoffset"] = typeof(DateTimeOffsetRouteConstraint);
});

// ═══════════════════════════════════════════════════════════════════════════
// Environment-aware service registration
// ═══════════════════════════════════════════════════════════════════════════
builder.ConfigureServices()
    // Always register: JSON serialization, OpenAPI, core services
    .Always(static s => s
        .AddErrorOrOpenApi()
        .AddSingleton(TimeProvider.System)
        .AddScoped<ITodoService, TodoService>()
        .AddErrorOrEndpointJson<AppJsonSerializerContext>()
    )

    // Runtime only: real messaging infrastructure
    .Runtime(static s => s
        .AddMessaging()
        .AddSseStream<TodoCreatedEvent>()
        .AddSseStream<TodoCompletedEvent>())

    // Build-time only: null implementations for OpenAPI schema generation
    .Build(static s => s
        .AddNullMessaging()
        .AddNullSseStream<TodoCreatedEvent>()
        .AddNullSseStream<TodoCompletedEvent>());

var app = builder.Build();

// OpenAPI endpoint
app.MapOpenApi();

// Generated ErrorOr endpoints
app.MapErrorOrEndpoints();

// SSE streaming endpoints (100% AOT-safe with compile-time type verification)
app.MapSseAot<TodoCreatedEvent, AppJsonSerializerContext>("/api/events/todos/created", "todo.created");
app.MapSseAot<TodoCompletedEvent, AppJsonSerializerContext>("/api/events/todos/completed", "todo.completed");

app.Run();