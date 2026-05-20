using System.Reflection;
using StackExchange.Redis;

namespace Kronikol.Extensions.Redis;

/// <summary>
/// Tracking decorator for <see cref="IConnectionMultiplexer"/> that wraps databases
/// returned by <see cref="IConnectionMultiplexer.GetDatabase"/> with
/// <see cref="RedisTrackingDatabase"/> for test diagram tracking.
/// </summary>
public class RedisTrackingConnectionMultiplexer : DispatchProxy
{
    private IConnectionMultiplexer _inner = null!;
    private RedisTrackingDatabaseOptions _options = null!;

    public static IConnectionMultiplexer Create(
        IConnectionMultiplexer inner,
        RedisTrackingDatabaseOptions options)
    {
        var proxy = Create<IConnectionMultiplexer, RedisTrackingConnectionMultiplexer>();
        var wrapper = (RedisTrackingConnectionMultiplexer)(object)proxy;
        wrapper._inner = inner;
        wrapper._options = options;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
            throw new ArgumentNullException(nameof(targetMethod));

        var result = targetMethod.Invoke(_inner, args);

        // Intercept GetDatabase() to wrap the returned IDatabase
        if (targetMethod.Name == nameof(IConnectionMultiplexer.GetDatabase) && result is IDatabase db)
        {
            return RedisTrackingDatabase.Create(db, _options);
        }

        return result;
    }
}
