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
                    Scenarios = featureGroup
                        .DistinctBy(x => x.TestId)
                        .OrderByDescending(x => x.Tags.Contains(BDDfyConstants.HappyPathTag, StringComparer.OrdinalIgnoreCase))
                        .ThenBy(x => x.ScenarioTitle)
                        .Select(x => new Scenario
                        {
                            Id = x.TestId,
                            DisplayName = x.ScenarioTitle,
                            IsHappyPath = x.Tags.Contains(BDDfyConstants.HappyPathTag, StringComparer.OrdinalIgnoreCase),
                            Result = x.Result.ToScenarioResult(),
                        }).ToArray()
                };
            }).ToArray();
    }
}
