namespace TestTrackingDiagrams.Extensions.Redis;

public record RedisTrackingDatabaseOptions
{
    public string ServiceName { get; set; } = "Redis";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = "Caller";

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

    public RedisTrackingVerbosity Verbosity { get; set; } = RedisTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public RedisTrackingVerbosity? SetupVerbosity { get; set; }
    public RedisTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public Microsoft.AspNetCore.Http.IHttpContextAccessor? HttpContextAccessor { get; set; }
}
