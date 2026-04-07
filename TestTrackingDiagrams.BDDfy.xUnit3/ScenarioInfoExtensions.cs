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
                    DisplayName = featureGroup.Key,
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
                                Result = x.Result.ToScenarioResult(),
                                Duration = x.Duration != TimeSpan.Zero ? x.Duration : null,
                                Steps = x.Steps.Count > 0
                                    ? x.Steps.Select(s => new ScenarioStep { Keyword = s.Keyword, Text = s.Text }).ToArray()
                                    : null,
                                Labels = labels.Length > 0 ? labels : null,
                            };
                        }).ToArray())
                };
            }).ToArray();
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
