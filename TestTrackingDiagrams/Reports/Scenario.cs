namespace TestTrackingDiagrams.Reports;

public record Scenario
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public bool IsHappyPath { get; set; }
    public ScenarioResult Result { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
}