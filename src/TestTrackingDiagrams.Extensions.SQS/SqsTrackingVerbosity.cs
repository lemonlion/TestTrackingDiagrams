namespace TestTrackingDiagrams.Extensions.SQS;

/// <summary>
/// Controls how much detail the SQS tracking extension includes in diagram entries.
/// </summary>
public enum SqsTrackingVerbosity
{
    /// <summary>Full detail including raw content, headers, and connection information.</summary>
    Raw,
    /// <summary>Classified labels with relevant context (e.g. operation name, target resource).</summary>
    Detailed,
    /// <summary>Minimal labels only — content and connection details are omitted.</summary>
    Summarised
}
