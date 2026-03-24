using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.xUnit2;

internal static class ScenarioInfoCollectionExtensions
{
    public static Feature[] ToFeatures(this IEnumerable<ScenarioInfo> scenarios)
    {
        return scenarios
            .OrderBy(x => x.FeatureName)
            .GroupBy(x => x.FeatureName)
            .Select(scenariosForFeature =>
            {
                var first = scenariosForFeature.First();
                return new Feature
                {
                    DisplayName = first.FeatureName,
                    Endpoint = first.Endpoint,
                    Scenarios = scenariosForFeature
                        .OrderByDescending(x => x.IsHappyPath)
                        .ThenBy(x => x.ScenarioName)
                        .Select(x => new Scenario
                        {
                            Id = x.Id,
                            Result = x.Result,
                            DisplayName = x.ScenarioName,
                            IsHappyPath = x.IsHappyPath,
                            ErrorMessage = x.ErrorMessage ?? string.Empty,
                            ErrorStackTrace = x.ErrorStackTrace ?? string.Empty,
                        }).ToArray()
                };
            }).ToArray();
    }
}
