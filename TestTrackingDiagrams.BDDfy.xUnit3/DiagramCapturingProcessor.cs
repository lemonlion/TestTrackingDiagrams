using TestStack.BDDfy;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public class DiagramCapturingProcessor : IProcessor
{
    public ProcessType ProcessType => ProcessType.Report;

    public void Process(Story story)
    {
        var testId = Xunit.TestContext.Current.Test?.UniqueID;
        if (testId == null) return;

        var storyTitle = story.Metadata?.Title
            ?? story.Namespace?.Split('.').LastOrDefault()
            ?? "Unknown Feature";

        var storyDescription = BuildStoryDescription(story.Metadata);

        foreach (var scenario in story.Scenarios)
        {
            var steps = scenario.Steps
                .Where(s => s.ShouldReport)
                .OrderBy(s => s.ExecutionOrder)
                .Select(s => s.ToBDDfyStepInfo())
                .ToList();

            BDDfyScenarioCollector.Collect(new BDDfyScenarioInfo
            {
                TestId = testId,
                BDDfyScenarioId = scenario.Id,
                StoryTitle = storyTitle,
                StoryDescription = storyDescription,
                ScenarioTitle = scenario.Title,
                Tags = scenario.Tags?.ToArray() ?? [],
                Steps = steps,
                Result = scenario.Result,
                Duration = scenario.Duration
            });
        }
    }

    private static string? BuildStoryDescription(StoryMetadata? metadata)
    {
        if (metadata == null) return null;

        var parts = new[] { metadata.Narrative1, metadata.Narrative2, metadata.Narrative3 }
            .Where(n => !string.IsNullOrWhiteSpace(n));

        var description = string.Join("\n", parts);
        return string.IsNullOrWhiteSpace(description) ? null : description;
    }
}
