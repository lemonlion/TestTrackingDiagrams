using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.EventHubs;

namespace TestTrackingDiagrams.Tests.EventHubs;

public class EventHubsServiceCollectionExtensionsTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==;EntityPath=my-hub";

    // ─── Producer ───────────────────────────────────────────

    [Fact]
    public void AddEventHubsProducerTestTracking_decorates_registered_producer()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new EventHubProducerClient(FakeConnectionString));

        services.AddEventHubsProducerTestTracking(options =>
        {
            options.ServiceName = "TestHub";
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<EventHubProducerClient>();

        Assert.IsType<TrackingEventHubProducerClient>(client);
    }

    [Fact]
    public void AddEventHubsProducerTestTracking_preserves_service_lifetime()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new EventHubProducerClient(FakeConnectionString));

        services.AddEventHubsProducerTestTracking();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(EventHubProducerClient));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddEventHubsProducerTestTracking_is_noop_when_no_producer_registered()
    {
        var services = new ServiceCollection();

        services.AddEventHubsProducerTestTracking();

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(EventHubProducerClient));
    }

    [Fact]
    public void AddEventHubsProducerTestTracking_preserves_inner_client()
    {
        var services = new ServiceCollection();
        var realClient = new EventHubProducerClient(FakeConnectionString);
        services.AddSingleton(realClient);

        services.AddEventHubsProducerTestTracking();

        var provider = services.BuildServiceProvider();
        var tracked = Assert.IsType<TrackingEventHubProducerClient>(
            provider.GetRequiredService<EventHubProducerClient>());
        Assert.Same(realClient, tracked.Inner);
    }

    // ─── Consumer ───────────────────────────────────────────

    [Fact]
    public void AddEventHubsConsumerTestTracking_decorates_registered_consumer()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new EventHubConsumerClient("$Default", FakeConnectionString));

        services.AddEventHubsConsumerTestTracking(options =>
        {
            options.ServiceName = "TestHub";
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<EventHubConsumerClient>();

        Assert.IsType<TrackingEventHubConsumerClient>(client);
    }

    [Fact]
    public void AddEventHubsConsumerTestTracking_preserves_service_lifetime()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new EventHubConsumerClient("$Default", FakeConnectionString));

        services.AddEventHubsConsumerTestTracking();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(EventHubConsumerClient));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddEventHubsConsumerTestTracking_is_noop_when_no_consumer_registered()
    {
        var services = new ServiceCollection();

        services.AddEventHubsConsumerTestTracking();

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(EventHubConsumerClient));
    }

    // ─── Combined ──────────────────────────────────────────

    [Fact]
    public void AddEventHubsTestTracking_decorates_both_producer_and_consumer()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new EventHubProducerClient(FakeConnectionString));
        services.AddSingleton(new EventHubConsumerClient("$Default", FakeConnectionString));

        services.AddEventHubsTestTracking();

        var provider = services.BuildServiceProvider();
        Assert.IsType<TrackingEventHubProducerClient>(provider.GetRequiredService<EventHubProducerClient>());
        Assert.IsType<TrackingEventHubConsumerClient>(provider.GetRequiredService<EventHubConsumerClient>());
    }
}
