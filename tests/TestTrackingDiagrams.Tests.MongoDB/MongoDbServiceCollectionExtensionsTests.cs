using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.MongoDB;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.MongoDB;

public class MongoDbServiceCollectionExtensionsTests : IDisposable
{
    public MongoDbServiceCollectionExtensionsTests()
    {
        TrackingComponentRegistry.Clear();
    }

    public void Dispose()
    {
        TrackingComponentRegistry.Clear();
    }

    [Fact]
    public void AddMongoDbTestTracking_Registers_MongoDbTrackingSubscriber()
    {
        var services = new ServiceCollection();

        services.AddMongoDbTestTracking();

        var provider = services.BuildServiceProvider();
        var subscriber = provider.GetService<MongoDbTrackingSubscriber>();
        Assert.NotNull(subscriber);
    }

    [Fact]
    public void AddMongoDbTestTracking_Registers_As_Singleton()
    {
        var services = new ServiceCollection();

        services.AddMongoDbTestTracking();

        var descriptor = services.Single(d => d.ServiceType == typeof(MongoDbTrackingSubscriber));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddMongoDbTestTracking_Applies_Options()
    {
        var services = new ServiceCollection();

        services.AddMongoDbTestTracking(opts =>
        {
            opts.ServiceName = "MyMongo";
            opts.Verbosity = MongoDbTrackingVerbosity.Summarised;
        });

        var provider = services.BuildServiceProvider();
        var subscriber = provider.GetRequiredService<MongoDbTrackingSubscriber>();
        Assert.Contains("MyMongo", subscriber.ComponentName);
    }

    [Fact]
    public void AddMongoDbTestTracking_Returns_ServiceCollection_For_Chaining()
    {
        var services = new ServiceCollection();

        var result = services.AddMongoDbTestTracking();

        Assert.Same(services, result);
    }
}
