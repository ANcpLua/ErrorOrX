using System.Reflection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Fluent API for environment-aware service registration.
/// Enables conditional registration during build-time OpenAPI generation vs runtime.
/// </summary>
public static class EnvironmentAwareHostingExtensions
{
    /// <summary>
    /// Starts a fluent chain for environment-aware service registration.
    /// </summary>
    public static IEnvironmentAwareBuilder ConfigureServices(this IHostApplicationBuilder builder) =>
        new EnvironmentAwareBuilder(builder.Services, builder.Environment);

    /// <summary>
    /// Starts a fluent chain for environment-aware service registration.
    /// </summary>
    public static IEnvironmentAwareBuilder ForEnvironment(
        this IServiceCollection services,
        IHostEnvironment environment) =>
        new EnvironmentAwareBuilder(services, environment);

    /// <summary>
    /// Detects build-time OpenAPI document generation (GetDocument.Insider tool).
    /// </summary>
    public static bool IsBuild(this IHostEnvironment environment) =>
        environment.IsEnvironment("Build") ||
        Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}

/// <summary>
/// Fluent builder for environment-conditional service registration.
/// </summary>
public interface IEnvironmentAwareBuilder
{
    /// <summary>Registers services only at runtime (skipped during build-time OpenAPI generation).</summary>
    IEnvironmentAwareBuilder Runtime(Action<IServiceCollection> configure);

    /// <summary>Registers services only during build-time OpenAPI generation.</summary>
    IEnvironmentAwareBuilder Build(Action<IServiceCollection> configure);

    /// <summary>Registers services in all environments.</summary>
    IEnvironmentAwareBuilder Always(Action<IServiceCollection> configure);

    /// <summary>Registers services when the predicate matches.</summary>
    IEnvironmentAwareBuilder When(Func<IHostEnvironment, bool> predicate, Action<IServiceCollection> configure);

    /// <summary>Returns the service collection for continued chaining.</summary>
    IServiceCollection Services { get; }
}

file sealed class EnvironmentAwareBuilder : IEnvironmentAwareBuilder
{
    private readonly IHostEnvironment _environment;
    private readonly bool _isBuild;

    public EnvironmentAwareBuilder(IServiceCollection services, IHostEnvironment environment)
    {
        Services = services;
        _environment = environment;
        _isBuild = environment.IsBuild();
    }

    public IServiceCollection Services { get; }

    public IEnvironmentAwareBuilder Runtime(Action<IServiceCollection> configure)
    {
        if (!_isBuild) configure(Services);
        return this;
    }

    public IEnvironmentAwareBuilder Build(Action<IServiceCollection> configure)
    {
        if (_isBuild) configure(Services);
        return this;
    }

    public IEnvironmentAwareBuilder Always(Action<IServiceCollection> configure)
    {
        configure(Services);
        return this;
    }

    public IEnvironmentAwareBuilder When(Func<IHostEnvironment, bool> predicate, Action<IServiceCollection> configure)
    {
        if (predicate(_environment)) configure(Services);
        return this;
    }
}