namespace TestTrackingDiagrams.Reports;

public record ScenarioStep
{
    public string? Keyword { get; set; }
    public required string Text { get; set; }
    public ScenarioResult? Status { get; set; }
    public TimeSpan? Duration { get; set; }
    public ScenarioStep[]? SubSteps { get; set; }
    public StepParameter[]? Parameters { get; set; }
    public string[]? Comments { get; set; }
    public FileAttachment[]? Attachments { get; set; }
}
