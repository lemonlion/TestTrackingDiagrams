using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.ServiceBus;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.ServiceBus;

[Collection("TrackingComponentRegistry")]
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

    // ─── Action<> overload ──────────────────────────────────

    [Fact]
    public void AddServiceBusTestTracking_decorates_registered_client()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA=="));

        services.AddServiceBusTestTracking(options =>
        {
            options.ServiceName = "TestBus";
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ServiceBusClient>();
        var tracked = Assert.IsType<TrackingServiceBusClient>(client);
        Assert.Equal("fake.servicebus.windows.net", tracked.FullyQualifiedNamespace);
    }

    [Fact]
    public void AddServiceBusTestTracking_preserves_service_lifetime()
    {
        var services = new ServiceCollection();
        services.AddScoped<ServiceBusClient>(_ =>
            new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA=="));

        services.AddServiceBusTestTracking();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ServiceBusClient));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddServiceBusTestTracking_does_not_duplicate_registrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA=="));

        services.AddServiceBusTestTracking();

        Assert.Single(services, d => d.ServiceType == typeof(ServiceBusClient));
    }

    [Fact]
    public void AddServiceBusTestTracking_is_noop_when_no_client_registered()
    {
        var services = new ServiceCollection();

        services.AddServiceBusTestTracking();

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(ServiceBusClient));
    }

    [Fact]
    public void AddServiceBusTestTracking_applies_options_configuration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA=="));

        services.AddServiceBusTestTracking(options =>
        {
            options.ServiceName = "CustomBus";
            options.CallingServiceName = "MySvc";
            options.Verbosity = ServiceBusTrackingVerbosity.Summarised;
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ServiceBusClient>();
        Assert.IsType<TrackingServiceBusClient>(client);
    }

    [Fact]
    public void AddServiceBusTestTracking_preserves_inner_client()
    {
        var services = new ServiceCollection();
        var realClient = new ServiceBusClient("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=dGVzdA==");
        services.AddSingleton(realClient);

        services.AddServiceBusTestTracking();

        var provider = services.BuildServiceProvider();
        var tracked = Assert.IsType<TrackingServiceBusClient>(provider.GetRequiredService<ServiceBusClient>());
        Assert.Same(realClient, tracked.Inner);
    }

    [Fact]
    public void AddServiceBusTestTracking_resolves_IHttpContextAccessor_from_DI()
    {
        var services = new ServiceCollection();
        var accessor = new HttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(accessor);
        services.AddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA=="));

        services.AddServiceBusTestTracking();

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ServiceBusClient>();
        Assert.IsType<TrackingServiceBusClient>(client);
    }

    // ─── Legacy overload (ServiceBusTrackingOptions) ────────

    [Fact]
    public void AddServiceBusTestTracking_legacy_decorates_registered_client()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient("Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA=="));

        services.AddServiceBusTestTracking(MakeOptions());

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ServiceBusClient>();
        var tracked = Assert.IsType<TrackingServiceBusClient>(client);
        Assert.Equal("fake.servicebus.windows.net", tracked.FullyQualifiedNamespace);
    }

    [Fact]
    public void AddServiceBusTestTracking_legacy_is_noop_when_no_client_registered()
    {
        var services = new ServiceCollection();

        var ex = Record.Exception(() => services.AddServiceBusTestTracking(MakeOptions()));

        Assert.Null(ex);
    }
}
