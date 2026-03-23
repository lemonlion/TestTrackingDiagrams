namespace TestTrackingDiagrams.Reports;

public record Scenario
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public bool IsHappyPath { get; set; }
    public ScenarioResult Result { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
}