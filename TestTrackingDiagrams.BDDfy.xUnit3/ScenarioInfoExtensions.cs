using TestTrackingDiagrams;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

internal static class ScenarioInfoExtensions
{
    public static Feature[] ToFeatures(this IEnumerable<BDDfyScenarioInfo> scenarios)
    {
        return scenarios
            .GroupBy(x => x.StoryTitle)
            .OrderBy(x => x.Key)
            .Select(featureGroup =>
            {
                var firstScenario = featureGroup.First();
                var endpoint = featureGroup
                    .SelectMany(s => s.Tags)
                    .FirstOrDefault(t => t.StartsWith(BDDfyConstants.EndpointTagPrefix, StringComparison.OrdinalIgnoreCase))
                    ?[BDDfyConstants.EndpointTagPrefix.Length..];

                return new Feature
                {
                    DisplayName = featureGroup.Key.Titleize(),
                    Endpoint = endpoint,
                    Description = firstScenario.StoryDescription,
                    Scenarios = DeduplicateScenarioTitles(featureGroup
                        .DistinctBy(x => x.TestId)
                        .OrderByDescending(x => x.Tags.Contains(BDDfyConstants.HappyPathTag, StringComparer.OrdinalIgnoreCase))
                        .ThenBy(x => x.ScenarioTitle)
                        .Select(x =>
                        {
                            var labels = x.Tags
                                .Where(t => !t.Equals(BDDfyConstants.HappyPathTag, StringComparison.OrdinalIgnoreCase)
                                         && !t.StartsWith(BDDfyConstants.EndpointTagPrefix, StringComparison.OrdinalIgnoreCase))
                                .ToArray();

                            return new Scenario
                            {
                                Id = x.TestId,
                                DisplayName = x.ScenarioTitle,
                                IsHappyPath = x.Tags.Contains(BDDfyConstants.HappyPathTag, StringComparer.OrdinalIgnoreCase),
                                Result = x.Result.ToExecutionResult(),
                                ErrorMessage = x.ErrorMessage,
                                ErrorStackTrace = x.ErrorStackTrace,
                                Duration = x.Duration != TimeSpan.Zero ? x.Duration : null,
                                Steps = x.Steps.Count > 0
                                    ? MapSteps(x.Steps)
                                    : null,
                                Labels = labels.Length > 0 ? labels : null,
                            };
                        }).ToArray())
                };
            }).ToArray();
    }

    private static ScenarioStep[] MapSteps(List<BDDfyStepInfo> steps)
    {
        var mapped = new ScenarioStep[steps.Count];
        var priorFailure = false;
        for (var i = 0; i < steps.Count; i++)
        {
            mapped[i] = new ScenarioStep
            {
                Keyword = steps[i].Keyword,
                Text = steps[i].Text,
                Status = steps[i].Result.ToStepResult(priorFailure),
                Duration = steps[i].Duration,
            };
            if (steps[i].Result == TestStack.BDDfy.Result.Failed)
                priorFailure = true;
        }
        return mapped;
    }

    private static Scenario[] DeduplicateScenarioTitles(Scenario[] scenarios)
    {
        var titleCounts = scenarios.GroupBy(s => s.DisplayName).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        if (titleCounts.Count == 0) return scenarios;

        var titleIndex = new Dictionary<string, int>();
        foreach (var scenario in scenarios)
        {
            if (!titleCounts.Contains(scenario.DisplayName)) continue;

            if (!titleIndex.TryGetValue(scenario.DisplayName, out var index))
                index = 0;
            titleIndex[scenario.DisplayName] = index + 1;

            scenario.DisplayName = $"{scenario.DisplayName} ({index + 1})";
        }

        return scenarios;
    }
}
