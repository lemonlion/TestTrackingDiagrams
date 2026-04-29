namespace TestTrackingDiagrams.ComponentDiagram;

/// <summary>
/// Represents a dependency relationship between a calling service and a target service,
/// with aggregated call statistics and protocol information.
/// </summary>
public record ComponentRelationship(
    string Caller,
    string Service,
    string Protocol,
    HashSet<string> Methods,
    int CallCount,
    int TestCount,
    string? DependencyCategory = null);
