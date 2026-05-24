var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
    .AddErrorOrOpenApi()
    .AddValidation()
    .AddSingleton(TimeProvider.System)
    .AddScoped<ITodoService, TodoService>()
    .AddScoped<IOrderService, OrderService>();

builder.Services.AddErrorOrEndpoints()
    .UseJsonContext<AppJsonSerializerContext>()
    .WithCamelCase()
    .WithIgnoreNulls();

var app = builder.Build();

app.MapOpenApi();
app.MapErrorOrEndpoints();

app.Run();
