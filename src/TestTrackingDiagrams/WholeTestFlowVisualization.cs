namespace TestTrackingDiagrams;

/// <summary>
/// Controls which internal-flow visualizations are generated alongside sequence diagrams.
/// </summary>
public enum WholeTestFlowVisualization
{
    /// <summary>No internal-flow visualization is generated.</summary>
    None,

    /// <summary>Generates a flame chart showing span durations as stacked bars.</summary>
    FlameChart,

    /// <summary>Generates a PlantUML activity diagram from collected spans.</summary>
    ActivityDiagram,

    /// <summary>Generates both a flame chart and an activity diagram.</summary>
    Both
}
