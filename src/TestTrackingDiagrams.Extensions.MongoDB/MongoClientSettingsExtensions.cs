using global::MongoDB.Driver;

namespace TestTrackingDiagrams.Extensions.MongoDB;

public static class MongoClientSettingsExtensions
{
    /// <summary>
    /// Adds MongoDB command tracking for test diagrams.
    /// </summary>
    public static MongoClientSettings WithTestTracking(
        this MongoClientSettings settings,
        MongoDbTrackingOptions options)
    {
        var subscriber = new MongoDbTrackingSubscriber(options);

        var existingConfigurator = settings.ClusterConfigurator;
        settings.ClusterConfigurator = builder =>
        {
            existingConfigurator?.Invoke(builder);
            subscriber.Subscribe(builder);
        };

        return settings;
    }
}
