using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.PubSub.Tests;

public class PubSubServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPubSubTestTracking_registers_PubSubTracker()
    {
        var services = new ServiceCollection();

        services.AddPubSubTestTracking(options =>
        {
            options.ServiceName = "TestPubSub";
        });

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetRequiredService<PubSubTracker>();

        Assert.NotNull(tracker);
    }

    [Fact]
    public void AddPubSubTestTracking_applies_options()
    {
        var services = new ServiceCollection();

        services.AddPubSubTestTracking(options =>
        {
            options.ServiceName = "CustomPubSub";
            options.CallerName = "MySvc";
            options.Verbosity = PubSubTrackingVerbosity.Summarised;
        });

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetRequiredService<PubSubTracker>();

        Assert.Equal("PubSubTracker (CustomPubSub)", tracker.ComponentName);
    }

    [Fact]
    public void AddPubSubTestTracking_registers_as_singleton()
    {
        var services = new ServiceCollection();

        services.AddPubSubTestTracking();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(PubSubTracker));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddPubSubTestTracking_resolves_same_instance_across_scopes()
    {
        var services = new ServiceCollection();
        services.AddPubSubTestTracking();

        var provider = services.BuildServiceProvider();
        var tracker1 = provider.GetRequiredService<PubSubTracker>();

        using var scope = provider.CreateScope();
        var tracker2 = scope.ServiceProvider.GetRequiredService<PubSubTracker>();

        Assert.Same(tracker1, tracker2);
    }

    [Fact]
    public void AddPubSubTestTracking_with_default_options()
    {
        var services = new ServiceCollection();

        services.AddPubSubTestTracking();

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetRequiredService<PubSubTracker>();

        Assert.Equal("PubSubTracker (PubSub)", tracker.ComponentName);
    }
}
