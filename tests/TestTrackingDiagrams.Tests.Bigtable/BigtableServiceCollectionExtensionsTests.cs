using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.Bigtable;

namespace TestTrackingDiagrams.Tests.Bigtable;

public class BigtableServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBigtableTestTracking_Registers_BigtableTracker()
    {
        var services = new ServiceCollection();

        services.AddBigtableTestTracking();

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetService<BigtableTracker>();
        Assert.NotNull(tracker);
    }

    [Fact]
    public void AddBigtableTestTracking_Registers_As_Singleton()
    {
        var services = new ServiceCollection();

        services.AddBigtableTestTracking();

        var descriptor = services.Single(d => d.ServiceType == typeof(BigtableTracker));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddBigtableTestTracking_Applies_Options()
    {
        var services = new ServiceCollection();

        services.AddBigtableTestTracking(opts =>
        {
            opts.ServiceName = "MyBigtable";
            opts.Verbosity = BigtableTrackingVerbosity.Summarised;
        });

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetRequiredService<BigtableTracker>();
        Assert.Contains("MyBigtable", tracker.ComponentName);
    }

    [Fact]
    public void AddBigtableTestTracking_Returns_ServiceCollection_For_Chaining()
    {
        var services = new ServiceCollection();

        var result = services.AddBigtableTestTracking();

        Assert.Same(services, result);
    }
}
