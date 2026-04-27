namespace TestTrackingDiagrams;

/// <summary>
/// Controls how GraphQL request bodies are displayed in sequence diagram notes.
/// </summary>
public enum GraphQlBodyFormat
{
    /// <summary>Current default: JSON pretty-print. The query value stays as a single-line string.</summary>
    Json,

    /// <summary>Formatted GraphQL query only; HTTP headers and JSON metadata (variables, extensions) are suppressed.</summary>
    FormattedQueryOnly,

    /// <summary>Formatted GraphQL query with HTTP headers shown above.</summary>
    Formatted,

    /// <summary>Formatted GraphQL query with HTTP headers, plus variables/extensions sections below. This is the default.</summary>
    FormattedWithMetadata
}
