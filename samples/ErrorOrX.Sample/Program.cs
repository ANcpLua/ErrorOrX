var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
    .AddOpenApi()
    .AddSingleton(TimeProvider.System)
    .AddScoped<ITodoService, TodoService>();

var app = builder.Build();

app.MapOpenApi();
app.MapErrorOrEndpoints();

app.Run();