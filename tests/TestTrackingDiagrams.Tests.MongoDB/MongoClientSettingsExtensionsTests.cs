using MongoDB.Driver;
using TestTrackingDiagrams.Extensions.MongoDB;

namespace TestTrackingDiagrams.Tests.MongoDB;

public class MongoClientSettingsExtensionsTests
{
    [Fact]
    public void WithTestTracking_ReturnsSameSettingsInstance()
    {
        var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
        var options = new MongoDbTrackingOptions();

        var result = settings.WithTestTracking(options);

        Assert.Same(settings, result);
    }

    [Fact]
    public void WithTestTracking_SetsClusterConfigurator()
    {
        var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
        var options = new MongoDbTrackingOptions();

        settings.WithTestTracking(options);

        Assert.NotNull(settings.ClusterConfigurator);
    }

    [Fact]
    public void WithTestTracking_PreservesExistingClusterConfigurator()
    {
        var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
        Action<global::MongoDB.Driver.Core.Configuration.ClusterBuilder> existing = _ => { };
        settings.ClusterConfigurator = existing;
        var options = new MongoDbTrackingOptions();

        settings.WithTestTracking(options);

        // Verify the configurator was replaced (chained, not the original)
        Assert.NotNull(settings.ClusterConfigurator);
        Assert.NotSame(existing, settings.ClusterConfigurator);
    }
}
