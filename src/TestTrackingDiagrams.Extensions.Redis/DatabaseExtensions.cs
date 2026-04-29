using StackExchange.Redis;

namespace TestTrackingDiagrams.Extensions.Redis;

/// <summary>
/// Provides extension methods for wrapping Redis database instances with test tracking.
/// </summary>
public static class DatabaseExtensions
{
    public static IDatabase WithRedisTestTracking(this IDatabase database, RedisTrackingDatabaseOptions options)
        => RedisTrackingDatabase.Create(database, options);

    public static IDatabase GetTrackedDatabase(this IConnectionMultiplexer multiplexer, RedisTrackingDatabaseOptions options, int db = -1)
        => multiplexer.GetDatabase(db).WithRedisTestTracking(options);
}