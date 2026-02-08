var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
    .AddErrorOrOpenApi()
    .AddSingleton(TimeProvider.System)
    .AddScoped<ITodoService, TodoService>();

// builder pattern (like ASP.NET Core's AddRazorComponents)
builder.Services.AddErrorOrEndpoints()
    .UseJsonContext<AppJsonSerializerContext>()
    .WithCamelCase()
    .WithIgnoreNulls();

var app = builder.Build();

app.MapOpenApi();

// convention builder return (like ASP.NET Core's MapRazorComponents)
app.MapErrorOrEndpoints();


app.Run();
