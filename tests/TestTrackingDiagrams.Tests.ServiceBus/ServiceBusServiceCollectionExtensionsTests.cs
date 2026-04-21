using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.ServiceBus;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.ServiceBus;

public class ServiceBusServiceCollectionExtensionsTests : IDisposable
{
    public ServiceBusServiceCollectionExtensionsTests()
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
    public void AddServiceBusTestTracking_RegistersOptions()
    {
        var services = new ServiceCollection();
        var options = MakeOptions();

        services.AddServiceBusTestTracking(options);

        var sp = services.BuildServiceProvider();
        var resolved = sp.GetRequiredService<ServiceBusTrackingOptions>();
        Assert.Same(options, resolved);
    }

    [Fact]
    public void AddServiceBusTestTracking_WithExistingClient_ReplacesWithTrackingClient()
    {
        var services = new ServiceCollection();
        // Register a factory for ServiceBusClient (can't create without connection string, 
        // but we can verify the registration mechanics)
        services.AddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA=="));

        services.AddServiceBusTestTracking(MakeOptions());

        var sp = services.BuildServiceProvider();
        var tracked = sp.GetRequiredService<TrackingServiceBusClient>();
        Assert.NotNull(tracked);
        Assert.Equal("fake.servicebus.windows.net", tracked.FullyQualifiedNamespace);
    }

    [Fact]
    public void AddServiceBusTestTracking_WithExistingClient_RemovesOriginalRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA=="));

        services.AddServiceBusTestTracking(MakeOptions());

        // ServiceBusClient registration should be gone, only TrackingServiceBusClient
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(ServiceBusClient));
    }

    [Fact]
    public void AddServiceBusTestTracking_WithInstanceRegistration_WrapsInstance()
    {
        var services = new ServiceCollection();
        var client = new ServiceBusClient("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=dGVzdA==");
        services.AddSingleton(client);

        services.AddServiceBusTestTracking(MakeOptions());

        var sp = services.BuildServiceProvider();
        var tracked = sp.GetRequiredService<TrackingServiceBusClient>();
        Assert.Same(client, tracked.Inner);
    }

    [Fact]
    public void AddServiceBusTestTracking_WithoutExistingClient_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var ex = Record.Exception(() => services.AddServiceBusTestTracking(MakeOptions()));

        Assert.Null(ex);
    }
}
