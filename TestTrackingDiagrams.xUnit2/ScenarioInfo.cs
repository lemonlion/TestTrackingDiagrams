using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.xUnit2;

public class ScenarioInfo
{
    public required string Id { get; init; }
    public required string FeatureName { get; init; }
    public required string ScenarioName { get; set; }
    public required string MethodMatchKey { get; init; }
    public string? Endpoint { get; init; }
    public bool IsHappyPath { get; init; }
    public ExecutionResult Result { get; set; } = ExecutionResult.Passed;
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public TimeSpan? Duration { get; set; }
}
