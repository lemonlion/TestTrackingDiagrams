namespace TestTrackingDiagrams;

/// <summary>
/// Controls where the flame chart is positioned relative to the sequence diagram.
/// </summary>
public enum InternalFlowFlameChartPosition
{
    /// <summary>The flame chart appears below the sequence diagram.</summary>
    Underneath,

    /// <summary>The flame chart overlays the diagram area and is revealed via a toggle button.</summary>
    BehindWithToggle
}
