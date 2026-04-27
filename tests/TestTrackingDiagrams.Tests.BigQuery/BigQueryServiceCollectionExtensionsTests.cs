using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.BigQuery;

namespace TestTrackingDiagrams.Tests.BigQuery;

public class BigQueryServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBigQueryTestTracking_Registers_BigQueryTracker()
    {
        var services = new ServiceCollection();

        services.AddBigQueryTestTracking();

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetService<BigQueryTracker>();
        Assert.NotNull(tracker);
    }

    [Fact]
    public void AddBigQueryTestTracking_Registers_As_Singleton()
    {
        var services = new ServiceCollection();

        services.AddBigQueryTestTracking();

        var descriptor = services.Single(d => d.ServiceType == typeof(BigQueryTracker));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddBigQueryTestTracking_Applies_Options()
    {
        var services = new ServiceCollection();

        services.AddBigQueryTestTracking(opts =>
        {
            opts.ServiceName = "MyBQ";
            opts.Verbosity = BigQueryTrackingVerbosity.Summarised;
        });

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetRequiredService<BigQueryTracker>();
        Assert.Contains("MyBQ", tracker.ComponentName);
    }

    [Fact]
    public void AddBigQueryTestTracking_Returns_ServiceCollection_For_Chaining()
    {
        var services = new ServiceCollection();

        var result = services.AddBigQueryTestTracking();

        Assert.Same(services, result);
    }
}
