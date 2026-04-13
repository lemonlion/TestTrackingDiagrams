namespace TestTrackingDiagrams.BDDfy.xUnit3;

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
}

public record BDDfyStepInfo(string Keyword, string Text, TestStack.BDDfy.Result Result = TestStack.BDDfy.Result.NotExecuted);
