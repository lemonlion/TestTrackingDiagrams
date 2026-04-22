namespace TestTrackingDiagrams.ComponentDiagram;

/// <summary>
/// Controls whether component diagram arrows are colored by dependency type or performance (P95 latency).
/// </summary>
public enum ArrowColorMode
{
    /// <summary>Arrow color indicates the target service's dependency type (default).</summary>
    DependencyType,

    /// <summary>Arrow color indicates P95 latency (green/orange/red).</summary>
    Performance
}
