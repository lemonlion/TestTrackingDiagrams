namespace TestTrackingDiagrams.Reports;

/// <summary>
/// Represents a single test scenario in the report, including execution result,
/// steps, labels, categories, and parameterized example values.
/// </summary>
public record Scenario
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public bool IsHappyPath { get; set; }
    public ExecutionResult Result { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public TimeSpan? Duration { get; set; }
    public ScenarioStep[]? Steps { get; set; }
    public string[]? Labels { get; set; }
    public string[]? Categories { get; set; }
    public string? Rule { get; set; }
    public string? OutlineId { get; set; }
    public Dictionary<string, string>? ExampleValues { get; set; }
    public Dictionary<string, object?>? ExampleRawValues { get; set; }
    public string? ExampleDisplayName { get; set; }
}