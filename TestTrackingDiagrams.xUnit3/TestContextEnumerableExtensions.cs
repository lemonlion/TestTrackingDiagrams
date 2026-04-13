using TestTrackingDiagrams.Reports;
using Xunit;

namespace TestTrackingDiagrams.xUnit3;

internal static class TestContextEnumerableExtensions
{
    public static Feature[] ToFeatures(this IEnumerable<ITestContext> contexts)
    {
        return contexts
            .Where(x => x.Test is not null && x.TestClass is not null)
            .OrderBy(x => x.TestClass!.TestClassSimpleName)
            .GroupBy(x => x.TestClass!.TestClassSimpleName)
            .Select(scenariosForFeature =>
            {
                var featureClass = scenariosForFeature.First().TestClass!;
                return new Feature
                {
                    DisplayName = DisplayNameFormatter.FormatFeatureName(scenariosForFeature.Key),
                    Endpoint = featureClass.Traits.SingleOrDefault(y => y.Key == EndpointAttribute.EndpointTraitKey).Value?.FirstOrDefault(),
                    Scenarios = scenariosForFeature
                        .DistinctBy(x => x.Test!.UniqueID)
                        .OrderByDescending(x => x.TestMethod!.Traits.ContainsKey(HappyPathAttribute.HappyPathTraitKey))
                        .ThenBy(x => x.Test?.TestDisplayName)
                        .Select(x => new Scenario
                        {
                            Id = x.Test!.UniqueID,
                            Result = x.TestState!.Result.ToExecutionResult(),
                            DisplayName = DisplayNameFormatter.FormatScenarioDisplayName(x.Test!.TestDisplayName),
                            IsHappyPath = x.Test!.Traits.ContainsKey(HappyPathAttribute.HappyPathTraitKey),
                            ErrorMessage = string.Join(Environment.NewLine, x.TestState!.FailureCause) + Environment.NewLine + string.Join(Environment.NewLine, x.TestState!.ExceptionMessages ?? []),
                            ErrorStackTrace = string.Join(Environment.NewLine, x.TestState!.ExceptionStackTraces ?? []),
                            Duration = x.TestState!.ExecutionTime is > 0 ? TimeSpan.FromMilliseconds((double)x.TestState.ExecutionTime.Value) : null
                        }).ToArray()
                };
            }).ToArray();
    }
}