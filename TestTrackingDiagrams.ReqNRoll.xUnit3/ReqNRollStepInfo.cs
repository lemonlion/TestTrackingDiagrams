using Reqnroll;

namespace TestTrackingDiagrams.ReqNRoll.xUnit3;

public record ReqNRollStepInfo(
    string Keyword,
    string Text,
    ScenarioExecutionStatus Status = ScenarioExecutionStatus.OK,
    TimeSpan? Duration = null,
    string? TableText = null,
    string? DocString = null);
