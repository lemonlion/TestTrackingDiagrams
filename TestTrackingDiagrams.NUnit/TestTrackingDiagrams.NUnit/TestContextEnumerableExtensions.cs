using TestTrackingDiagrams.Reports;
using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit;

internal static class TestContextEnumerableExtensions
{
    public static Feature[] ToFeatures(this IEnumerable<TestContext> contexts)
    {
        return contexts
            .GroupBy(x => x.Test.DisplayName)
            .OrderBy(x => x.Key)
            .Select(scenariosForFeature =>
            {
                var firstScenarioForFeature = scenariosForFeature.First();

                string? endpoint = null;
                if (firstScenarioForFeature.Test.Parent!.Properties.ContainsKey(EndpointAttribute.EndpointPropertyKey))
                    endpoint = firstScenarioForFeature.Test.Parent.Properties[EndpointAttribute.EndpointPropertyKey][0]?.ToString();

                return new Feature
                {
                    DisplayName = firstScenarioForFeature.Test.DisplayName!.Replace("_", " "),
                    Endpoint = endpoint,
                    Scenarios = scenariosForFeature
                        .DistinctBy(x => x.Test.ID)
                        .OrderByDescending(x => x.Test.Properties.ContainsKey(HappyPathAttribute.HappyPathPropertyKey))
                        .ThenBy(x => x.Test.MethodName)
                        .Select(x => new Scenario
                        {
                            Id = x.Test.ID,
                            Result = x.Result.Outcome.Status.ToScenarioResult(),
                            DisplayName = x.Test.MethodName!.Replace("_", " "),
                            IsHappyPath = x.Test.Properties.ContainsKey(HappyPathAttribute.HappyPathPropertyKey),
                            ErrorMessage = x.Result.Message,
                            ErrorStackTrace = x.Result.StackTrace
                        }).ToArray()
                };
            }).ToArray();
    }
}
