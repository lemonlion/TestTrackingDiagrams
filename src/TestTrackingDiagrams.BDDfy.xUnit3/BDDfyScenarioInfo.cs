namespace TestTrackingDiagrams.BDDfy.xUnit3;

/// <summary>
/// Contains complete metadata for a BDDfy scenario execution, including story context, steps, tags, and results.
/// </summary>
public record BDDfyScenarioInfo
{
    public required string TestId { get; init; }
    public string? BDDfyScenarioId { get; init; }
    public required string StoryTitle { get; init; }
    public string? StoryDescription { get; init; }
    public required string ScenarioTitle { get; init; }
    public required string[] Tags { get; init; }
    public required List<BDDfyStepInfo> Steps { get; init; }
    public required TestStack.BDDfy.Result Result { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorStackTrace { get; init; }
}

/// <summary>
/// Captures information about an individual BDDfy step execution, including its keyword, text, result, and duration.
/// </summary>
public record BDDfyStepInfo(string Keyword, string Text, TestStack.BDDfy.Result Result = TestStack.BDDfy.Result.NotExecuted, TimeSpan? Duration = null);