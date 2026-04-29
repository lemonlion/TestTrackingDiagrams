namespace TestTrackingDiagrams.Extensions.Redis;

/// <summary>
/// The result of classifying a Redis operation, containing the operation type and metadata.
/// </summary>
public record RedisOperationInfo(
    RedisOperation Operation,
    RedisCacheResult CacheResult,
    string? Key,
    int DatabaseNumber);
