namespace TestTrackingDiagrams.ComponentDiagram;

public record DependencyGraphMetrics(
    ServiceMetrics[] Services,
    string[][] CircularDependencies,
    int LongestChainLength,
    string[] LongestChain);

public record ServiceMetrics(
    string Name,
    int FanIn,
    int FanOut,
    string[] InboundFrom,
    string[] OutboundTo);
