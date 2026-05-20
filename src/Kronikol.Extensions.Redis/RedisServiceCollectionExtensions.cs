using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Kronikol.Tracking;

namespace Kronikol.Extensions.Redis;

/// <summary>
/// Extension methods for registering Redis test tracking via dependency injection.
/// </summary>
public static class RedisServiceCollectionExtensions
{
    /// <summary>
    /// Decorates all existing <see cref="IDatabase"/> registrations with
    /// <see cref="RedisTrackingDatabase"/> for test diagram tracking.
    /// <para>
    /// An <see cref="IHttpContextAccessor"/> is resolved from DI (if registered) and wired
    /// into the tracking options automatically.
    /// </para>
    /// <para>No-op when no matching registrations exist.</para>
    /// </summary>
    public static IServiceCollection AddRedisTestTracking(
        this IServiceCollection services,
        Action<RedisTrackingDatabaseOptions>? configure = null)
    {
        var options = new RedisTrackingDatabaseOptions();
        configure?.Invoke(options);

        services.DecorateAll<IDatabase>((sp, inner) =>
        {
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            return RedisTrackingDatabase.Create(inner, options);
        });

        return services;
    }

    /// <summary>
    /// Decorates all existing <see cref="IConnectionMultiplexer"/> registrations so that
    /// <see cref="IConnectionMultiplexer.GetDatabase"/> returns tracked <see cref="IDatabase"/>
    /// instances for test diagram tracking.
    /// <para>
    /// Use this when your application uses a Redis wrapper library that manages its own
    /// <see cref="IConnectionMultiplexer"/> internally and doesn't register <see cref="IDatabase"/>
    /// directly in DI.
    /// </para>
    /// <para>No-op when no matching registrations exist.</para>
    /// </summary>
    public static IServiceCollection AddRedisConnectionMultiplexerTracking(
        this IServiceCollection services,
        Action<RedisTrackingDatabaseOptions>? configure = null)
    {
        var options = new RedisTrackingDatabaseOptions();
        configure?.Invoke(options);

        services.DecorateAll<IConnectionMultiplexer>((sp, inner) =>
        {
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            return RedisTrackingConnectionMultiplexer.Create(inner, options);
        });

        return services;
    }
}
