namespace Kronikol.Extensions.Spanner;

/// <summary>
/// Controls the level of detail included in response content for diagram arrows.
/// </summary>
public enum SpannerResponseDetail
{
    /// <summary>Row count only (e.g. "3 rows")</summary>
    RowCountOnly,

    /// <summary>Row count + column names (e.g. "3 rows [Name, Preference, CreatedAt]")</summary>
    RowCountAndColumns,

    /// <summary>Full row data up to MaxResponseRows (JSON-like representation)</summary>
    FullRows
}
