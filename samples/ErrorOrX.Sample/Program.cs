var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
    .AddOpenApi()
    .AddSingleton(TimeProvider.System)
    .AddScoped<ITodoService, TodoService>();

// New builder pattern (like ASP.NET Core's AddRazorComponents)
builder.Services.AddErrorOrEndpoints()
    .UseJsonContext<AppJsonSerializerContext>()
    .WithCamelCase()
    .WithIgnoreNulls();

var app = builder.Build();

app.MapOpenApi();

// New convention builder return (like ASP.NET Core's MapRazorComponents)
app.MapErrorOrEndpoints();

app.Run();
