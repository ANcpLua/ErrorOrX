using Microsoft.AspNetCore.RateLimiting;

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

// Services for AdminApi.cs middleware attributes ([Authorize], [EnableRateLimiting],
// [OutputCache], [EnableCors]). No authentication scheme is registered — wire your
// own to actually exercise [Authorize] endpoints (they 401 in this sample).
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", static p => p.RequireAuthenticatedUser());

builder.Services.AddRateLimiter(static options =>
    options.AddFixedWindowLimiter("fixed", static o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
    }));

builder.Services.AddOutputCache();

builder.Services.AddCors(static options =>
    options.AddPolicy("Public", static p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();

app.MapOpenApi();
app.MapErrorOrEndpoints();

app.Run();
