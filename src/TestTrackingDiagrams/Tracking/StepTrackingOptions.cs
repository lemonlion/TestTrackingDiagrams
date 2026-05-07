namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Configuration options for step tracking behavior.
/// Set via <see cref="StepCollector.Options"/> static property.
/// </summary>
public record StepTrackingOptions
{
    /// <summary>Whether to prepend keyword ("Given", "When", "Then") to step text in reports. Default: true.</summary>
    public bool PrependKeyword { get; set; } = true;

    /// <summary>Whether to include parameter values inline in step text. Default: true.</summary>
    public bool InlineParameters { get; set; } = true;

    /// <summary>Whether [WhenStep] triggers Action phase transition (equivalent to StartAction()). Default: true.</summary>
    public bool WhenTriggersAction { get; set; } = true;
}
