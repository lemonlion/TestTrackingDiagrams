using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.Grpc;

namespace TestTrackingDiagrams.Tests.Grpc;

public class GrpcServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTrackedGrpcClient_registers_client_as_singleton()
    {
        var services = new ServiceCollection();

        services.AddTrackedGrpcClient<StubGrpcClient>(
            new TestHttpMessageHandler(),
            new Uri("http://localhost"),
            opts => opts.ServiceName = "Test Service");

        var descriptor = services.Single(d => d.ServiceType == typeof(StubGrpcClient));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddTrackedGrpcClient_resolves_client_from_DI()
    {
        var services = new ServiceCollection();

        services.AddTrackedGrpcClient<StubGrpcClient>(
            new TestHttpMessageHandler(),
            new Uri("http://localhost"),
            opts => opts.ServiceName = "Test Service");

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<StubGrpcClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddTrackedGrpcClient_auto_resolves_HttpContextAccessor_from_DI()
    {
        var testAccessor = new TestHttpContextAccessor(new DefaultHttpContext());
        GrpcTrackingOptions? capturedOptions = null;

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(testAccessor);

        services.AddTrackedGrpcClient<StubGrpcClient>(
            new TestHttpMessageHandler(),
            new Uri("http://localhost"),
            opts =>
            {
                opts.ServiceName = "Test Service";
                // NOT setting HttpContextAccessor — should be auto-resolved
                capturedOptions = opts;
            });

        var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<StubGrpcClient>(); // trigger factory

        Assert.NotNull(capturedOptions);
        Assert.Same(testAccessor, capturedOptions!.HttpContextAccessor);
    }

    [Fact]
    public void AddTrackedGrpcClient_does_not_override_explicit_HttpContextAccessor()
    {
        var explicitAccessor = new TestHttpContextAccessor(new DefaultHttpContext());
        var diAccessor = new TestHttpContextAccessor(new DefaultHttpContext());
        GrpcTrackingOptions? capturedOptions = null;

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(diAccessor);

        services.AddTrackedGrpcClient<StubGrpcClient>(
            new TestHttpMessageHandler(),
            new Uri("http://localhost"),
            opts =>
            {
                opts.ServiceName = "Test Service";
                opts.HttpContextAccessor = explicitAccessor;
                capturedOptions = opts;
            });

        var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<StubGrpcClient>(); // trigger factory

        Assert.NotNull(capturedOptions);
        Assert.Same(explicitAccessor, capturedOptions!.HttpContextAccessor);
    }

    [Fact]
    public void AddTrackedGrpcClient_works_without_HttpContextAccessor_in_DI()
    {
        var services = new ServiceCollection();

        services.AddTrackedGrpcClient<StubGrpcClient>(
            new TestHttpMessageHandler(),
            new Uri("http://localhost"),
            opts => opts.ServiceName = "Test Service");

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<StubGrpcClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddTrackedGrpcClient_returns_service_collection_for_fluent_chaining()
    {
        var services = new ServiceCollection();

        var result = services.AddTrackedGrpcClient<StubGrpcClient>(
            new TestHttpMessageHandler(),
            new Uri("http://localhost"));

        Assert.Same(services, result);
    }

    [Fact]
    public void AddTrackedGrpcClient_works_without_configure_callback()
    {
        var services = new ServiceCollection();

        services.AddTrackedGrpcClient<StubGrpcClient>(
            new TestHttpMessageHandler(),
            new Uri("http://localhost"));

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<StubGrpcClient>();

        Assert.NotNull(client);
    }

    public class StubGrpcClient : ClientBase<StubGrpcClient>
    {
        public StubGrpcClient(CallInvoker callInvoker) : base(callInvoker) { }
        protected StubGrpcClient(ClientBaseConfiguration configuration) : base(configuration) { }
        protected override StubGrpcClient NewInstance(ClientBaseConfiguration configuration) => new(configuration);
    }

    private class TestHttpContextAccessor : IHttpContextAccessor
    {
        public TestHttpContextAccessor(HttpContext? httpContext) => HttpContext = httpContext;
        public HttpContext? HttpContext { get; set; }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
