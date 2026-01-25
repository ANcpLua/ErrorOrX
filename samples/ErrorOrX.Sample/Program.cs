var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
    .AddOpenApi()
    .AddSingleton(TimeProvider.System)
    .AddScoped<ITodoService, TodoService>()
    .AddErrorOrEndpoints(options => options
        .UseJsonContext<AppJsonSerializerContext>());

var app = builder.Build();

app.MapOpenApi();
app.MapErrorOrEndpoints();

app.Run();
