using ErrorOr.Http.Bcl.Sample;
using ErrorOr.Http.Bcl.Sample.Domain;
using ErrorOr.Http.Bcl.Sample.Infrastructure.Messaging;
using ErrorOr.Http.Generated;

var builder = WebApplication.CreateSlimBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
// Environment-aware service registration
// ═══════════════════════════════════════════════════════════════════════════
builder.ConfigureServices()
    // Always register: JSON serialization, OpenAPI, core services
    .Always(s => s
        .AddErrorOrEndpointJson<AppJsonSerializerContext>()
        .AddErrorOrOpenApi()
        .AddSingleton(TimeProvider.System)
        .AddScoped<ITodoService, TodoService>())

    // Runtime only: real messaging infrastructure
    .Runtime(s => s
        .AddMessaging()
        .AddSseStream<TodoCreatedEvent>()
        .AddSseStream<TodoCompletedEvent>())

    // Build-time only: null implementations for OpenAPI schema generation
    .Build(s => s
        .AddNullMessaging()
        .AddNullSseStream<TodoCreatedEvent>()
        .AddNullSseStream<TodoCompletedEvent>());

var app = builder.Build();

// OpenAPI endpoint
app.MapOpenApi();

// Generated ErrorOr endpoints
app.MapErrorOrEndpoints();

// SSE streaming endpoints (work at runtime, schema-only at build-time)
#pragma warning disable IL2026, IL3050 // Generic SSE mapping uses reflection
app.MapSse<TodoCreatedEvent>("/api/events/todos/created", "todo.created");
app.MapSse<TodoCompletedEvent>("/api/events/todos/completed", "todo.completed");
#pragma warning restore IL2026, IL3050

app.Run();
