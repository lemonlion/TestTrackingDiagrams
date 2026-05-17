using Kronikol.Constants;
namespace Kronikol.Extensions.Spanner;

/// <summary>
/// Configuration options for Google Cloud Spanner test tracking.
/// </summary>
public record SpannerTrackingOptions
{
    public string ServiceName { get; set; } = "Spanner";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = TrackingDefaults.CallerName;

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

    public SpannerTrackingVerbosity Verbosity { get; set; } = SpannerTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public bool LogSqlText { get; set; } = true;
    [Obsolete("Raw verbosity now always includes parameters. This property is no longer used.")]
    public bool LogParameters { get; set; }
    public HashSet<SpannerOperation> ExcludedOperations { get; set; } = [];
    public SpannerTrackingVerbosity? SetupVerbosity { get; set; }
    public SpannerTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;

    /// <summary>
    /// Whether to include response content in diagrams. Default: true.
    /// When false, response arrows are empty (previous behaviour).
    /// Response arrows show payload data (row counts, column names, etc.) at all verbosity levels.
    /// </summary>
    public bool LogResponseContent { get; set; } = true;

    /// <summary>
    /// Maximum number of rows to include in response content.
    /// Default: 5. Set to 0 for row count only (overrides ResponseDetail for row data).
    /// Negative values are treated as 0.
    /// </summary>
    public int MaxResponseRows { get; set; } = 5;

    /// <summary>
    /// Level of detail for response content in diagram arrows.
    /// Default: RowCountAndColumns.
    /// </summary>
    public SpannerResponseDetail ResponseDetail { get; set; } = SpannerResponseDetail.RowCountAndColumns;
}