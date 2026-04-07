using Reqnroll;

namespace TestTrackingDiagrams.ReqNRoll.xUnit2;

public record ReqNRollScenarioInfo
{
    public required string ScenarioId { get; init; }
    public required string ScenarioTitle { get; init; }
    public required string FeatureTitle { get; init; }
    public string? FeatureDescription { get; init; }
    public required string[] ScenarioTags { get; init; }
    public required string[] CombinedTags { get; init; }
    public Exception? TestError { get; init; }
    public ScenarioExecutionStatus ExecutionStatus { get; init; }
    public TimeSpan? Duration { get; init; }
    public List<ReqNRollStepInfo> Steps { get; init; } = [];
}
