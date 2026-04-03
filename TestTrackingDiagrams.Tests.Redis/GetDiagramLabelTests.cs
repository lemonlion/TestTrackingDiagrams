using TestTrackingDiagrams.Extensions.Redis;

namespace TestTrackingDiagrams.Tests.Redis;

public class GetDiagramLabelTests
{
    // ─── Detailed verbosity ─────────────────────────────────

    [Theory]
    [InlineData(RedisOperation.Get, RedisCacheResult.Hit, "Get (Hit)")]
    [InlineData(RedisOperation.Get, RedisCacheResult.Miss, "Get (Miss)")]
    [InlineData(RedisOperation.Set, RedisCacheResult.None, "Set")]
    [InlineData(RedisOperation.Delete, RedisCacheResult.None, "Delete")]
    [InlineData(RedisOperation.KeyExists, RedisCacheResult.None, "KeyExists")]
    [InlineData(RedisOperation.Expire, RedisCacheResult.None, "Expire")]
    [InlineData(RedisOperation.HashGet, RedisCacheResult.Hit, "HashGet (Hit)")]
    [InlineData(RedisOperation.HashGet, RedisCacheResult.Miss, "HashGet (Miss)")]
    [InlineData(RedisOperation.HashSet, RedisCacheResult.None, "HashSet")]
    [InlineData(RedisOperation.HashDelete, RedisCacheResult.None, "HashDelete")]
    [InlineData(RedisOperation.HashGetAll, RedisCacheResult.None, "HashGetAll")]
    [InlineData(RedisOperation.ListPush, RedisCacheResult.None, "ListPush")]
    [InlineData(RedisOperation.ListRange, RedisCacheResult.None, "ListRange")]
    [InlineData(RedisOperation.SetAdd, RedisCacheResult.None, "SetAdd")]
    [InlineData(RedisOperation.SetMembers, RedisCacheResult.None, "SetMembers")]
    [InlineData(RedisOperation.Increment, RedisCacheResult.None, "Increment")]
    [InlineData(RedisOperation.Decrement, RedisCacheResult.None, "Decrement")]
    [InlineData(RedisOperation.Publish, RedisCacheResult.None, "Publish")]
    [InlineData(RedisOperation.Other, RedisCacheResult.None, "Other")]
    public void Detailed_ReturnsExpectedLabel(RedisOperation op, RedisCacheResult cache, string expected)
    {
        var info = new RedisOperationInfo(op, cache, "key", 0);
        var label = RedisOperationClassifier.GetDiagramLabel(info, RedisTrackingVerbosity.Detailed);
        Assert.Equal(expected, label);
    }

    // ─── Summarised verbosity (same labels as detailed) ─────

    [Theory]
    [InlineData(RedisOperation.Get, RedisCacheResult.Hit, "Get (Hit)")]
    [InlineData(RedisOperation.Get, RedisCacheResult.Miss, "Get (Miss)")]
    [InlineData(RedisOperation.Set, RedisCacheResult.None, "Set")]
    [InlineData(RedisOperation.HashGet, RedisCacheResult.Hit, "HashGet (Hit)")]
    [InlineData(RedisOperation.HashGet, RedisCacheResult.Miss, "HashGet (Miss)")]
    public void Summarised_ReturnsExpectedLabel(RedisOperation op, RedisCacheResult cache, string expected)
    {
        var info = new RedisOperationInfo(op, cache, "key", 0);
        var label = RedisOperationClassifier.GetDiagramLabel(info, RedisTrackingVerbosity.Summarised);
        Assert.Equal(expected, label);
    }

    // ─── Raw verbosity returns null (falls through to command name) ─

    [Theory]
    [InlineData(RedisOperation.Get, RedisCacheResult.Hit)]
    [InlineData(RedisOperation.Set, RedisCacheResult.None)]
    [InlineData(RedisOperation.HashGet, RedisCacheResult.Miss)]
    public void Raw_ReturnsNull(RedisOperation op, RedisCacheResult cache)
    {
        var info = new RedisOperationInfo(op, cache, "key", 0);
        var label = RedisOperationClassifier.GetDiagramLabel(info, RedisTrackingVerbosity.Raw);
        Assert.Null(label);
    }

    // ─── Hit/miss suffix only on read operations ────────────

    [Theory]
    [InlineData(RedisOperation.Get)]
    [InlineData(RedisOperation.HashGet)]
    public void HitMiss_OnlyOnReadOperations_Hit(RedisOperation op)
    {
        var info = new RedisOperationInfo(op, RedisCacheResult.Hit, "key", 0);
        var label = RedisOperationClassifier.GetDiagramLabel(info, RedisTrackingVerbosity.Detailed);
        Assert.Contains("(Hit)", label!);
    }

    [Theory]
    [InlineData(RedisOperation.Get)]
    [InlineData(RedisOperation.HashGet)]
    public void HitMiss_OnlyOnReadOperations_Miss(RedisOperation op)
    {
        var info = new RedisOperationInfo(op, RedisCacheResult.Miss, "key", 0);
        var label = RedisOperationClassifier.GetDiagramLabel(info, RedisTrackingVerbosity.Detailed);
        Assert.Contains("(Miss)", label!);
    }

    [Theory]
    [InlineData(RedisOperation.Set)]
    [InlineData(RedisOperation.Delete)]
    [InlineData(RedisOperation.HashSet)]
    [InlineData(RedisOperation.ListPush)]
    [InlineData(RedisOperation.Increment)]
    public void WriteOperations_NoHitMissSuffix(RedisOperation op)
    {
        var info = new RedisOperationInfo(op, RedisCacheResult.None, "key", 0);
        var label = RedisOperationClassifier.GetDiagramLabel(info, RedisTrackingVerbosity.Detailed);
        Assert.DoesNotContain("(Hit)", label!);
        Assert.DoesNotContain("(Miss)", label!);
    }
}
