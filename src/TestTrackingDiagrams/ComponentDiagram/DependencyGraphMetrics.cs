namespace TestTrackingDiagrams.ComponentDiagram;

/// <summary>
/// Graph-level metrics for the dependency graph, including fan-in/fan-out per service,
/// circular dependency detection, and longest dependency chain.
/// </summary>
public record DependencyGraphMetrics(
    ServiceMetrics[] Services,
    string[][] CircularDependencies,
    int LongestChainLength,
    string[] LongestChain);

/// <summary>
/// Contains metrics for an individual service in the dependency graph, including call counts, latency, and error rates.
/// </summary>
public record ServiceMetrics(
    string Name,
    int FanIn,
    int FanOut,
    string[] InboundFrom,
    string[] OutboundTo);