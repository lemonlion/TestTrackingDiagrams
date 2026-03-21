using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.ReqNRoll;

internal static class ScenarioInfoEnumerableExtensions
{
    public static Feature[] ToFeatures(this IEnumerable<ReqNRollScenarioInfo> scenarios)
    {
        return scenarios
            .GroupBy(x => x.FeatureTitle)
            .OrderBy(x => x.Key)
            .Select(featureGroup =>
            {
                var firstScenario = featureGroup.First();
                var endpoint = firstScenario.CombinedTags
                    .FirstOrDefault(t => t.StartsWith(ReqNRollConstants.EndpointTagPrefix, StringComparison.OrdinalIgnoreCase))
                    ?[ReqNRollConstants.EndpointTagPrefix.Length..];

                return new Feature
                {
                    DisplayName = featureGroup.Key,
                    Endpoint = endpoint,
                    Scenarios = featureGroup
                        .DistinctBy(x => x.ScenarioId)
                        .OrderByDescending(x => x.ScenarioTags.Contains(ReqNRollConstants.HappyPathTag, StringComparer.OrdinalIgnoreCase))
                        .ThenBy(x => x.ScenarioTitle)
                        .Select(x => new Scenario
                        {
                            Id = x.ScenarioId,
                            DisplayName = x.ScenarioTitle,
                            IsHappyPath = x.ScenarioTags.Contains(ReqNRollConstants.HappyPathTag, StringComparer.OrdinalIgnoreCase),
                            Result = x.ExecutionStatus.ToScenarioResult(),
                            ErrorMessage = x.TestError?.Message,
                            ErrorStackTrace = x.TestError?.StackTrace
                        }).ToArray()
                };
            }).ToArray();
    }
}
