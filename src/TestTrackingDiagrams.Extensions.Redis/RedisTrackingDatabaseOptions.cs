namespace TestTrackingDiagrams.Extensions.Redis;

public record RedisTrackingDatabaseOptions
{
    public string ServiceName { get; set; } = "Redis";
    public string CallingServiceName { get; set; } = "Caller";
    public RedisTrackingVerbosity Verbosity { get; set; } = RedisTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
}
