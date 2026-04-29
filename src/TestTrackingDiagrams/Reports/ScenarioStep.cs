namespace TestTrackingDiagrams.Reports;

/// <summary>
/// Represents a single step within a test scenario (e.g. Given, When, Then).
/// </summary>
public record ScenarioStep
{
    public string? Keyword { get; set; }
    public required string Text { get; set; }
    public ExecutionResult? Status { get; set; }
    public TimeSpan? Duration { get; set; }
    public ScenarioStep[]? SubSteps { get; set; }
    public StepParameter[]? Parameters { get; set; }
    public string[]? Comments { get; set; }
    public FileAttachment[]? Attachments { get; set; }
    public string? DocString { get; set; }
    public string? DocStringMediaType { get; set; }
}
