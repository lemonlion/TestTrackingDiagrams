namespace TestTrackingDiagrams.Sql;

/// <summary>
/// Controls how much detail SQL tracking extensions include in diagram entries.
/// </summary>
public enum SqlTrackingVerbosityLevel
{
    /// <summary>Full SQL text, parameters, and data source in the URI.</summary>
    Raw,

    /// <summary>Classified operation label with table name and data source URI.</summary>
    Detailed,

    /// <summary>Classified operation label only — SQL text and data source are omitted.</summary>
    Summarised
}
