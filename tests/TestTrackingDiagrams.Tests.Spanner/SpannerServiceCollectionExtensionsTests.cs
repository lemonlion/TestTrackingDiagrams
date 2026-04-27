using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.Spanner;

namespace TestTrackingDiagrams.Tests.Spanner;

public class SpannerServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSpannerTestTracking_Registers_SpannerTracker()
    {
        var services = new ServiceCollection();

        services.AddSpannerTestTracking();

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetService<SpannerTracker>();
        Assert.NotNull(tracker);
    }

    [Fact]
    public void AddSpannerTestTracking_Registers_As_Singleton()
    {
        var services = new ServiceCollection();

        services.AddSpannerTestTracking();

        var descriptor = services.Single(d => d.ServiceType == typeof(SpannerTracker));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddSpannerTestTracking_Applies_Options()
    {
        var services = new ServiceCollection();

        services.AddSpannerTestTracking(opts =>
        {
            opts.ServiceName = "MySpanner";
            opts.Verbosity = SpannerTrackingVerbosity.Summarised;
        });

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetRequiredService<SpannerTracker>();
        Assert.Contains("MySpanner", tracker.ComponentName);
    }

    [Fact]
    public void AddSpannerTestTracking_Returns_ServiceCollection_For_Chaining()
    {
        var services = new ServiceCollection();

        var result = services.AddSpannerTestTracking();

        Assert.Same(services, result);
    }
}
