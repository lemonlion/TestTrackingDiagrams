using StackExchange.Redis;

namespace TestTrackingDiagrams.Extensions.Redis;

public static class DatabaseExtensions
{
    public static IDatabase WithRedisTestTracking(this IDatabase database, RedisTrackingDatabaseOptions options)
        => RedisTrackingDatabase.Create(database, options);

    public static IDatabase GetTrackedDatabase(this IConnectionMultiplexer multiplexer, RedisTrackingDatabaseOptions options, int db = -1)
        => multiplexer.GetDatabase(db).WithRedisTestTracking(options);
}
