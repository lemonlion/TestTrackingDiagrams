namespace TestTrackingDiagrams.Extensions.Redis;

public record RedisOperationInfo(
    RedisOperation Operation,
    RedisCacheResult CacheResult,
    string? Key,
    int DatabaseNumber);
