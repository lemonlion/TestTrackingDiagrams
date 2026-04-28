using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.ServiceBus;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.ServiceBus;

[Collection("TrackingComponentRegistry")]
public class ServiceBusHttpContextAccessorAutoResolveTests : IDisposable
{
    public ServiceBusHttpContextAccessorAutoResolveTests()
    {
        TrackingComponentRegistry.Clear();
    }

    public void Dispose()
    {
        TrackingComponentRegistry.Clear();
    }

    private static ServiceBusTrackingOptions MakeOptions() => new()
    {
        ServiceName = "TestBus",
        CallingServiceName = "TestCaller",
        CurrentTestInfoFetcher = () => ("Test", Guid.NewGuid().ToString()),
    };

    [Fact]
    public void AddServiceBusTestTracking_auto_resolves_IHttpContextAccessor_from_DI()
    {
        var services = new ServiceCollection();
        var accessor = new HttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(accessor);
        services.AddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA=="));

        var options = MakeOptions();
        Assert.Null(options.HttpContextAccessor);

        services.AddServiceBusTestTracking(options);

        var sp = services.BuildServiceProvider();
        // Resolving the decorated client triggers the factory lambda
        var client = sp.GetRequiredService<ServiceBusClient>();
        var tracked = Assert.IsType<TrackingServiceBusClient>(client);

        Assert.NotNull(tracked);
        Assert.Same(accessor, options.HttpContextAccessor);
    }

    [Fact]
    public void AddServiceBusTestTracking_does_not_overwrite_explicit_HttpContextAccessor()
    {
        var services = new ServiceCollection();
        var diAccessor = new HttpContextAccessor();
        var explicitAccessor = new HttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(diAccessor);
        services.AddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA=="));

        var options = MakeOptions();
        options.HttpContextAccessor = explicitAccessor;

        services.AddServiceBusTestTracking(options);

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<ServiceBusClient>();
        var tracked = Assert.IsType<TrackingServiceBusClient>(client);

        Assert.NotNull(tracked);
        Assert.Same(explicitAccessor, options.HttpContextAccessor);
    }

    [Fact]
    public void AddServiceBusTestTracking_works_when_no_IHttpContextAccessor_registered()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA=="));

        var options = MakeOptions();
        services.AddServiceBusTestTracking(options);

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<ServiceBusClient>();
        var tracked = Assert.IsType<TrackingServiceBusClient>(client);

        Assert.NotNull(tracked);
        Assert.Null(options.HttpContextAccessor);
    }

    [Fact]
    public void ServiceBusTrackingOptions_HttpContextAccessor_defaults_to_null()
    {
        var options = new ServiceBusTrackingOptions();
        Assert.Null(options.HttpContextAccessor);
    }
}
