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
    string? DocString = null,
    InlineParamCapture[]? InlineParams = null);

/// <summary>
/// Captures a single inline parameter's position and value within the step text.
/// </summary>
public record InlineParamCapture(int StartOffset, int Length, string Value, string? Name);