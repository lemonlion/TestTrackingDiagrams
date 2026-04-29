using Microsoft.AspNetCore.Http;

namespace TestTrackingDiagrams.Sql;

/// <summary>
/// Shared options base for all direct SQL database tracking extensions.
/// </summary>
public record SqlTrackingOptionsBase
{
    public string ServiceName { get; set; } = "Database";
    public string CallingServiceName { get; set; } = "Caller";
    public SqlTrackingVerbosityLevel Verbosity { get; set; } = SqlTrackingVerbosityLevel.Detailed;
    public Func<(string Name, string Id)?>? CurrentTestInfoFetcher { get; set; }
    public IHttpContextAccessor? HttpContextAccessor { get; set; }
    public HashSet<UnifiedSqlOperation> ExcludedOperations { get; set; } = [];
    public bool LogParameters { get; set; }
    public bool LogSqlText { get; set; } = true;
    public SqlTrackingVerbosityLevel? SetupVerbosity { get; set; }
    public SqlTrackingVerbosityLevel? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;

    /// <summary>
    /// The DependencyCategory string used in <see cref="Tracking.RequestResponseLog.DependencyCategory"/>.
    /// Each database extension sets its own default (e.g. "PostgreSQL", "SqlServer", "MySQL").
    /// </summary>
    public string DependencyCategory { get; set; } = "SQL";

    /// <summary>
    /// URI scheme used for diagram URIs (e.g. "postgresql", "sqlserver", "mysql", "sqlite", "oracle").
    /// </summary>
    public string UriScheme { get; set; } = "sql";
}
