namespace TestTrackingDiagrams.Extensions.Redis;

/// <summary>
/// The cache outcome of a Redis operation.
/// </summary>
public enum RedisCacheResult
{
    /// <summary>The key existed and a value was returned.</summary>
    Hit,

    /// <summary>The key did not exist.</summary>
    Miss,

    /// <summary>Cache result is not applicable for this operation.</summary>
    None
}
