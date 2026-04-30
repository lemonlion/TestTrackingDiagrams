using TestTrackingDiagrams.Constants;
namespace TestTrackingDiagrams;

/// <summary>
/// Configuration options for Dapper test tracking.
/// </summary>
public record DapperTrackingOptions
{
    public string ServiceName { get; set; } = "Database";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = TrackingDefaults.CallerName;

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

    public DapperTrackingVerbosity Verbosity { get; set; } = DapperTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)?>? CurrentTestInfoFetcher { get; set; }
    public bool LogParameters { get; set; }
    public bool LogSqlText { get; set; } = true;
    public HashSet<DapperOperation> ExcludedOperations { get; set; } = [];
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public DapperTrackingVerbosity? SetupVerbosity { get; set; }
    public DapperTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public Microsoft.AspNetCore.Http.IHttpContextAccessor? HttpContextAccessor { get; set; }
}