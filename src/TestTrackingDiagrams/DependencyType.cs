namespace TestTrackingDiagrams;

/// <summary>
/// Broad classification of a dependency for visual differentiation in diagrams.
/// </summary>
public enum DependencyType
{
    HttpApi,
    Database,
    Cache,
    MessageQueue,
    Storage,
    Unknown
}
