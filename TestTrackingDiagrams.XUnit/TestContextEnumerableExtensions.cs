using TestTrackingDiagrams.Reports;
using Xunit;

namespace TestTrackingDiagrams.XUnit;

internal static class TestContextEnumerableExtensions
{
    public static Feature[] ToFeatures(this IEnumerable<ITestContext> contexts)
    {
        return contexts
            .Where(x => x.Test is not null && x.TestClass is not null)
            .OrderBy(x => x.TestClass!.TestClassSimpleName)
            .GroupBy(x => x.TestClass)
            .Select(scenariosForFeature =>
            {
                var feature = scenariosForFeature.Key!;
                return new Feature
                {
                    DisplayName = feature.TestClassSimpleName.Replace("_", " "),
                    Endpoint = feature.Traits.SingleOrDefault(y => y.Key == EndpointAttribute.EndpointTraitKey).Value?.FirstOrDefault(),
                    Scenarios = scenariosForFeature
                        .DistinctBy(x => x.Test!.UniqueID)
                        .OrderByDescending(x => x.TestMethod!.Traits.ContainsKey(HappyPathAttribute.HappyPathTraitKey))
                        .ThenBy(x => x.Test?.TestDisplayName)
                        .Select(x => new Scenario
                        {
                            Id = x.Test!.UniqueID,
                            Result = x.TestState!.Result.ToScenarioResult(),
                            DisplayName = x.Test!.TestDisplayName.Replace("_", " "),
                            IsHappyPath = x.Test!.Traits.ContainsKey(HappyPathAttribute.HappyPathTraitKey),
                            ErrorMessage = string.Join(Environment.NewLine, x.TestState!.FailureCause) + Environment.NewLine + string.Join(Environment.NewLine, x.TestState!.ExceptionMessages ?? []),
                            ErrorStackTrace = string.Join(Environment.NewLine, x.TestState!.ExceptionStackTraces ?? [])
                        }).ToArray()
                };
            }).ToArray();
    }
}