using global::MongoDB.Driver;

namespace TestTrackingDiagrams.Extensions.MongoDB;

/// <summary>
/// Provides extension methods for configuring MongoDB client options to enable test tracking.
/// </summary>
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