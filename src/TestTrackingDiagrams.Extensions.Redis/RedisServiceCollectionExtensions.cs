using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Redis;

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
}
