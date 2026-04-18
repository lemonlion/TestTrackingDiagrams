using TestTrackingDiagrams.Extensions.Redis;

namespace TestTrackingDiagrams.Tests.Redis;

public class RedisOperationClassifierTests
{
    // ─── String operations ──────────────────────────────────

    [Fact]
    public void Classify_StringGet_ReturnsGet()
    {
        var result = RedisOperationClassifier.Classify("GET", hasResult: true);
        Assert.Equal(RedisOperation.Get, result.Operation);
    }

    [Fact]
    public void Classify_StringGet_WithResult_ReturnsCacheHit()
    {
        var result = RedisOperationClassifier.Classify("GET", hasResult: true);
        Assert.Equal(RedisCacheResult.Hit, result.CacheResult);
    }

    [Fact]
    public void Classify_StringGet_WithoutResult_ReturnsCacheMiss()
    {
        var result = RedisOperationClassifier.Classify("GET", hasResult: false);
        Assert.Equal(RedisCacheResult.Miss, result.CacheResult);
    }

    [Fact]
    public void Classify_StringSet_ReturnsSet()
    {
        var result = RedisOperationClassifier.Classify("SET", hasResult: false);
        Assert.Equal(RedisOperation.Set, result.Operation);
    }

    [Fact]
    public void Classify_StringSet_ReturnsCacheResultNone()
    {
        var result = RedisOperationClassifier.Classify("SET", hasResult: false);
        Assert.Equal(RedisCacheResult.None, result.CacheResult);
    }

    [Fact]
    public void Classify_StringIncrement_ReturnsIncrement()
    {
        var result = RedisOperationClassifier.Classify("INCR", hasResult: true);
        Assert.Equal(RedisOperation.Increment, result.Operation);
    }

    [Fact]
    public void Classify_StringIncrementBy_ReturnsIncrement()
    {
        var result = RedisOperationClassifier.Classify("INCRBY", hasResult: true);
        Assert.Equal(RedisOperation.Increment, result.Operation);
    }

    [Fact]
    public void Classify_StringIncrementByFloat_ReturnsIncrement()
    {
        var result = RedisOperationClassifier.Classify("INCRBYFLOAT", hasResult: true);
        Assert.Equal(RedisOperation.Increment, result.Operation);
    }

    [Fact]
    public void Classify_StringDecrement_ReturnsDecrement()
    {
        var result = RedisOperationClassifier.Classify("DECR", hasResult: true);
        Assert.Equal(RedisOperation.Decrement, result.Operation);
    }

    [Fact]
    public void Classify_StringDecrementBy_ReturnsDecrement()
    {
        var result = RedisOperationClassifier.Classify("DECRBY", hasResult: true);
        Assert.Equal(RedisOperation.Decrement, result.Operation);
    }

    // ─── Key operations ─────────────────────────────────────

    [Fact]
    public void Classify_KeyDelete_ReturnsDelete()
    {
        var result = RedisOperationClassifier.Classify("DEL", hasResult: false);
        Assert.Equal(RedisOperation.Delete, result.Operation);
    }

    [Fact]
    public void Classify_Unlink_ReturnsDelete()
    {
        var result = RedisOperationClassifier.Classify("UNLINK", hasResult: false);
        Assert.Equal(RedisOperation.Delete, result.Operation);
    }

    [Fact]
    public void Classify_KeyExists_ReturnsKeyExists()
    {
        var result = RedisOperationClassifier.Classify("EXISTS", hasResult: true);
        Assert.Equal(RedisOperation.KeyExists, result.Operation);
    }

    [Fact]
    public void Classify_KeyExpire_ReturnsExpire()
    {
        var result = RedisOperationClassifier.Classify("EXPIRE", hasResult: false);
        Assert.Equal(RedisOperation.Expire, result.Operation);
    }

    [Fact]
    public void Classify_PExpire_ReturnsExpire()
    {
        var result = RedisOperationClassifier.Classify("PEXPIRE", hasResult: false);
        Assert.Equal(RedisOperation.Expire, result.Operation);
    }

    [Fact]
    public void Classify_ExpireAt_ReturnsExpire()
    {
        var result = RedisOperationClassifier.Classify("EXPIREAT", hasResult: false);
        Assert.Equal(RedisOperation.Expire, result.Operation);
    }

    [Fact]
    public void Classify_Persist_ReturnsExpire()
    {
        var result = RedisOperationClassifier.Classify("PERSIST", hasResult: false);
        Assert.Equal(RedisOperation.Expire, result.Operation);
    }

    // ─── Hash operations ────────────────────────────────────

    [Fact]
    public void Classify_HashGet_WithResult_ReturnsHashGetHit()
    {
        var result = RedisOperationClassifier.Classify("HGET", hasResult: true);
        Assert.Equal(RedisOperation.HashGet, result.Operation);
        Assert.Equal(RedisCacheResult.Hit, result.CacheResult);
    }

    [Fact]
    public void Classify_HashGet_WithoutResult_ReturnsHashGetMiss()
    {
        var result = RedisOperationClassifier.Classify("HGET", hasResult: false);
        Assert.Equal(RedisOperation.HashGet, result.Operation);
        Assert.Equal(RedisCacheResult.Miss, result.CacheResult);
    }

    [Fact]
    public void Classify_HGetAll_ReturnsHashGetAll()
    {
        var result = RedisOperationClassifier.Classify("HGETALL", hasResult: true);
        Assert.Equal(RedisOperation.HashGetAll, result.Operation);
    }

    [Fact]
    public void Classify_HMGet_ReturnsHashGet()
    {
        var result = RedisOperationClassifier.Classify("HMGET", hasResult: true);
        Assert.Equal(RedisOperation.HashGet, result.Operation);
    }

    [Fact]
    public void Classify_HashSet_ReturnsHashSet()
    {
        var result = RedisOperationClassifier.Classify("HSET", hasResult: false);
        Assert.Equal(RedisOperation.HashSet, result.Operation);
        Assert.Equal(RedisCacheResult.None, result.CacheResult);
    }

    [Fact]
    public void Classify_HMSet_ReturnsHashSet()
    {
        var result = RedisOperationClassifier.Classify("HMSET", hasResult: false);
        Assert.Equal(RedisOperation.HashSet, result.Operation);
    }

    [Fact]
    public void Classify_HashDelete_ReturnsHashDelete()
    {
        var result = RedisOperationClassifier.Classify("HDEL", hasResult: false);
        Assert.Equal(RedisOperation.HashDelete, result.Operation);
    }

    // ─── List operations ────────────────────────────────────

    [Fact]
    public void Classify_ListLeftPush_ReturnsListPush()
    {
        var result = RedisOperationClassifier.Classify("LPUSH", hasResult: false);
        Assert.Equal(RedisOperation.ListPush, result.Operation);
    }

    [Fact]
    public void Classify_ListRightPush_ReturnsListPush()
    {
        var result = RedisOperationClassifier.Classify("RPUSH", hasResult: false);
        Assert.Equal(RedisOperation.ListPush, result.Operation);
    }

    [Fact]
    public void Classify_ListRange_ReturnsListRange()
    {
        var result = RedisOperationClassifier.Classify("LRANGE", hasResult: true);
        Assert.Equal(RedisOperation.ListRange, result.Operation);
    }

    // ─── Set operations ─────────────────────────────────────

    [Fact]
    public void Classify_SetAdd_ReturnsSetAdd()
    {
        var result = RedisOperationClassifier.Classify("SADD", hasResult: false);
        Assert.Equal(RedisOperation.SetAdd, result.Operation);
    }

    [Fact]
    public void Classify_SetMembers_ReturnsSetMembers()
    {
        var result = RedisOperationClassifier.Classify("SMEMBERS", hasResult: true);
        Assert.Equal(RedisOperation.SetMembers, result.Operation);
    }

    // ─── Pub/Sub ────────────────────────────────────────────

    [Fact]
    public void Classify_Publish_ReturnsPublish()
    {
        var result = RedisOperationClassifier.Classify("PUBLISH", hasResult: false);
        Assert.Equal(RedisOperation.Publish, result.Operation);
    }

    // ─── Unknown commands ───────────────────────────────────

    [Fact]
    public void Classify_UnknownCommand_ReturnsOther()
    {
        var result = RedisOperationClassifier.Classify("RANDOMCOMMAND", hasResult: false);
        Assert.Equal(RedisOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_NullCommand_ReturnsOther()
    {
        var result = RedisOperationClassifier.Classify(null, hasResult: false);
        Assert.Equal(RedisOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_EmptyCommand_ReturnsOther()
    {
        var result = RedisOperationClassifier.Classify("", hasResult: false);
        Assert.Equal(RedisOperation.Other, result.Operation);
    }

    // ─── Case insensitivity ─────────────────────────────────

    [Fact]
    public void Classify_LowercaseGet_ReturnsGet()
    {
        var result = RedisOperationClassifier.Classify("get", hasResult: true);
        Assert.Equal(RedisOperation.Get, result.Operation);
    }

    [Fact]
    public void Classify_MixedCaseHSet_ReturnsHashSet()
    {
        var result = RedisOperationClassifier.Classify("hSet", hasResult: false);
        Assert.Equal(RedisOperation.HashSet, result.Operation);
    }

    // ─── Write operations have CacheResult.None ─────────────

    [Fact]
    public void Classify_Delete_ReturnsCacheResultNone()
    {
        var result = RedisOperationClassifier.Classify("DEL", hasResult: false);
        Assert.Equal(RedisCacheResult.None, result.CacheResult);
    }

    [Fact]
    public void Classify_Expire_ReturnsCacheResultNone()
    {
        var result = RedisOperationClassifier.Classify("EXPIRE", hasResult: false);
        Assert.Equal(RedisCacheResult.None, result.CacheResult);
    }

    [Fact]
    public void Classify_Publish_ReturnsCacheResultNone()
    {
        var result = RedisOperationClassifier.Classify("PUBLISH", hasResult: false);
        Assert.Equal(RedisCacheResult.None, result.CacheResult);
    }

    // ─── Non-read ops always return CacheResult.None ────────

    [Fact]
    public void Classify_Increment_ReturnsCacheResultNone()
    {
        var result = RedisOperationClassifier.Classify("INCR", hasResult: true);
        Assert.Equal(RedisCacheResult.None, result.CacheResult);
    }

    [Fact]
    public void Classify_KeyExists_ReturnsCacheResultNone()
    {
        var result = RedisOperationClassifier.Classify("EXISTS", hasResult: true);
        Assert.Equal(RedisCacheResult.None, result.CacheResult);
    }

    [Fact]
    public void Classify_ListRange_ReturnsCacheResultNone()
    {
        var result = RedisOperationClassifier.Classify("LRANGE", hasResult: true);
        Assert.Equal(RedisCacheResult.None, result.CacheResult);
    }

    [Fact]
    public void Classify_SetMembers_ReturnsCacheResultNone()
    {
        var result = RedisOperationClassifier.Classify("SMEMBERS", hasResult: true);
        Assert.Equal(RedisCacheResult.None, result.CacheResult);
    }

    [Fact]
    public void Classify_HashGetAll_ReturnsCacheResultNone()
    {
        var result = RedisOperationClassifier.Classify("HGETALL", hasResult: true);
        Assert.Equal(RedisCacheResult.None, result.CacheResult);
    }

    // ─── GETDEL and GETSET variants ─────────────────────────

    [Fact]
    public void Classify_GetDel_ReturnsGet_WithHitMiss()
    {
        var hit = RedisOperationClassifier.Classify("GETDEL", hasResult: true);
        Assert.Equal(RedisOperation.Get, hit.Operation);
        Assert.Equal(RedisCacheResult.Hit, hit.CacheResult);

        var miss = RedisOperationClassifier.Classify("GETDEL", hasResult: false);
        Assert.Equal(RedisCacheResult.Miss, miss.CacheResult);
    }

    [Fact]
    public void Classify_GetSet_ReturnsGet_WithHitMiss()
    {
        var hit = RedisOperationClassifier.Classify("GETSET", hasResult: true);
        Assert.Equal(RedisOperation.Get, hit.Operation);
        Assert.Equal(RedisCacheResult.Hit, hit.CacheResult);
    }

    [Fact]
    public void Classify_MGet_ReturnsGet()
    {
        var result = RedisOperationClassifier.Classify("MGET", hasResult: true);
        Assert.Equal(RedisOperation.Get, result.Operation);
    }

    [Fact]
    public void Classify_MSet_ReturnsSet()
    {
        var result = RedisOperationClassifier.Classify("MSET", hasResult: false);
        Assert.Equal(RedisOperation.Set, result.Operation);
    }

    [Fact]
    public void Classify_SetEx_ReturnsSet()
    {
        var result = RedisOperationClassifier.Classify("SETEX", hasResult: false);
        Assert.Equal(RedisOperation.Set, result.Operation);
    }

    [Fact]
    public void Classify_SetNx_ReturnsSet()
    {
        var result = RedisOperationClassifier.Classify("SETNX", hasResult: false);
        Assert.Equal(RedisOperation.Set, result.Operation);
    }

    [Fact]
    public void Classify_PSetEx_ReturnsSet()
    {
        var result = RedisOperationClassifier.Classify("PSETEX", hasResult: false);
        Assert.Equal(RedisOperation.Set, result.Operation);
    }
}
