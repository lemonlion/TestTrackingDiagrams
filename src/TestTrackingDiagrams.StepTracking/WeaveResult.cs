namespace TestTrackingDiagrams.StepTracking;

/// <summary>
/// Result of the step weaving operation.
/// </summary>
public class WeaveResult
{
    public int WeavedCount { get; set; }
    public string? SkipReason { get; set; }
}
