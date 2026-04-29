namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Controls how much detail <see cref="MessageTracker"/> includes in diagram entries.
/// </summary>
public enum MessageTrackerVerbosity
{
    /// <summary>Full detail including raw message payloads and all headers.</summary>
    Raw,

    /// <summary>Classified labels with message payloads included.</summary>
    Detailed,

    /// <summary>Minimal labels only — message payloads are omitted.</summary>
    Summarised
}
