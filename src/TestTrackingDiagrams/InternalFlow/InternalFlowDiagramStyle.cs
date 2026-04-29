namespace TestTrackingDiagrams;

/// <summary>
/// The visual style used to render internal-flow span data.
/// </summary>
public enum InternalFlowDiagramStyle
{
    /// <summary>Renders spans as a PlantUML activity diagram with swim lanes.</summary>
    ActivityDiagram,

    /// <summary>Renders spans as a hierarchical call tree.</summary>
    CallTree,

    /// <summary>Renders spans as a PlantUML sequence diagram.</summary>
    SequenceDiagram
}
