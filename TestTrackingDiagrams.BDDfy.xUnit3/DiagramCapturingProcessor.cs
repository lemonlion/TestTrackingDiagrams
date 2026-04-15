using System.Reflection;
using TestStack.BDDfy;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public class DiagramCapturingProcessor : IProcessor
{
    private static readonly FieldInfo? ScenarioTitleField =
        typeof(Scenario).GetField("<Title>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly PropertyInfo? ScenarioTitleProperty =
        typeof(Scenario).GetProperty(nameof(Scenario.Title));

    public ProcessType ProcessType => ProcessType.Report;

    public void Process(Story story)
    {
        var testContext = Xunit.TestContext.Current;
        var testId = testContext.Test?.UniqueID;
        if (testId == null) return;

        var storyTitle = story.Metadata?.Title
            ?? story.Namespace?.Split('.').LastOrDefault()
            ?? "Unknown Feature";

        var storyDescription = BuildStoryDescription(story.Metadata);

        var testClassSimpleName = testContext.TestClass?.TestClassSimpleName;
        var testMethodName = testContext.TestMethod?.MethodName;

        foreach (var scenario in story.Scenarios)
        {
            var resolvedTitle = ScenarioTitleResolver.ResolveScenarioTitle(
                scenario.Title, testClassSimpleName, testMethodName);

            // Fix the title in-place so BDDfy's own reporters see unique titles
            if (ScenarioTitleField != null)
                ScenarioTitleField.SetValue(scenario, resolvedTitle);
            else
                ScenarioTitleProperty?.SetValue(scenario, resolvedTitle);

            var steps = scenario.Steps
                .Where(s => s.ShouldReport)
                .OrderBy(s => s.ExecutionOrder)
                .Select(s => s.ToBDDfyStepInfo())
                .ToList();

            var failedException = scenario.Steps
                .FirstOrDefault(s => s.Result == TestStack.BDDfy.Result.Failed && s.Exception != null)
                ?.Exception;

            BDDfyScenarioCollector.Collect(new BDDfyScenarioInfo
            {
                TestId = testId,
                BDDfyScenarioId = scenario.Id,
                StoryTitle = storyTitle,
                StoryDescription = storyDescription,
                ScenarioTitle = resolvedTitle,
                Tags = scenario.Tags?.ToArray() ?? [],
                Steps = steps,
                Result = scenario.Result,
                Duration = scenario.Duration,
                ErrorMessage = failedException?.Message,
                ErrorStackTrace = failedException?.StackTrace
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
