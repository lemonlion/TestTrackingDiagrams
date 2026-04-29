using Reqnroll;

namespace TestTrackingDiagrams.ReqNRoll;

/// <summary>
/// Captures information about an individual Reqnroll step execution, including its keyword, text, status, duration, and associated data.
/// </summary>
public record ReqNRollStepInfo(
    string Keyword,
    string Text,
    ScenarioExecutionStatus Status = ScenarioExecutionStatus.OK,
    TimeSpan? Duration = null,
    string? TableText = null,
    string? DocString = null);