using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.MSTest;

internal static class TestContextEnumerableExtensions
{
    public static Feature[] ToFeatures(this IEnumerable<MSTestScenarioInfo> scenarioInfos)
    {
        return scenarioInfos
            .OrderBy(x => x.TestClassSimpleName)
            .GroupBy(x => x.TestClassSimpleName)
            .Select(scenariosForFeature =>
            {
                var firstScenario = scenariosForFeature.First();

                return new Feature
                {
                    DisplayName = firstScenario.TestClassSimpleName.Titleize(),
                    Endpoint = firstScenario.Endpoint,
                    Scenarios = scenariosForFeature
                        .DistinctBy(x => x.TestId)
                        .OrderByDescending(x => x.IsHappyPath)
                        .ThenBy(x => x.TestMethodName)
                        .Select(x => new Scenario
                        {
                            Id = x.TestId,
                            Result = x.Outcome.ToExecutionResult(),
                            DisplayName = x.TestMethodName.Replace("_", " "),
                            IsHappyPath = x.IsHappyPath,
                            ErrorMessage = x.ErrorMessage,
                            ErrorStackTrace = x.ErrorStackTrace,
                            Duration = x.Duration
                        }).ToArray()
                };
            }).ToArray();
    }
}
