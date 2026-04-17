using TestTrackingDiagrams.Reports;
using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

internal static class TestContextEnumerableExtensions
{
    public static Feature[] ToFeatures(this IEnumerable<TestContext> contexts)
    {
        return contexts
            .Where(x => x.Metadata.TestDetails.ClassType is not null)
            .OrderBy(x => x.Metadata.TestDetails.ClassType.Name)
            .GroupBy(x => x.Metadata.TestDetails.ClassType.Name)
            .Select(scenariosForFeature =>
            {
                var featureClass = scenariosForFeature.First();
                var categories = featureClass.Metadata.TestDetails.Categories;

                string? endpoint = null;
                if (featureClass.Metadata.TestDetails.CustomProperties.TryGetValue(EndpointAttribute.EndpointPropertyKey, out var endpointValue))
                    endpoint = endpointValue.FirstOrDefault();

                return new Feature
                {
                    DisplayName = DisplayNameFormatter.FormatFeatureName(scenariosForFeature.Key),
                    Endpoint = endpoint,
                    Scenarios = scenariosForFeature
                        .DistinctBy(x => x.Id)
                        .OrderByDescending(x => x.Metadata.TestDetails.Categories.Contains(HappyPathAttribute.HappyPathCategoryKey))
                        .ThenBy(x => x.Metadata.DisplayName)
                        .Select(x =>
                        {
                            var displayName = DisplayNameFormatter.FormatScenarioDisplayName(x.Metadata.DisplayName);
                            var parsed = ParameterParser.Parse(displayName);
                            return new Scenario
                            {
                                Id = x.Id,
                                Result = x.Execution.Result?.State.ToExecutionResult() ?? ExecutionResult.Skipped,
                                DisplayName = displayName,
                                IsHappyPath = x.Metadata.TestDetails.Categories.Contains(HappyPathAttribute.HappyPathCategoryKey),
                                ErrorMessage = x.Execution.Result?.Exception?.Message,
                                ErrorStackTrace = x.Execution.Result?.Exception?.StackTrace,
                                Duration = x.Execution.Result?.Duration,
                                OutlineId = parsed is { Count: > 0 } ? ParameterParser.ExtractBaseName(displayName) : null,
                                ExampleValues = parsed is { Count: > 0 } ? parsed : null
                            };
                        }).ToArray()
                };
            }).ToArray();
    }
}
