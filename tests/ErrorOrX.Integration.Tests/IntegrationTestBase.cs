namespace ErrorOrX.Integration.Tests;

public abstract class IntegrationTestBase : IClassFixture<IntegrationTestAppFactory>, IAsyncLifetime
{
    private readonly IntegrationTestAppFactory _factory;

    protected IntegrationTestBase(IntegrationTestAppFactory factory)
    {
        _factory = factory;
    }

    protected HttpClient Client { get; private set; } = null!;

    public virtual ValueTask InitializeAsync()
    {
        Client = _factory.CreateClient();
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }
}
