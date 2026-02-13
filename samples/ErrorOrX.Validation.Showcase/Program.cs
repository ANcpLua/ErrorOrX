var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
    .AddValidation()
    .AddSingleton(TimeProvider.System)
    .AddScoped<IOrderService, OrderService>();

builder.Services.AddErrorOrEndpoints()
    .UseJsonContext<AppJsonSerializerContext>()
    .WithCamelCase()
    .WithIgnoreNulls();

var app = builder.Build();

app.MapErrorOrEndpoints();

app.Run();
