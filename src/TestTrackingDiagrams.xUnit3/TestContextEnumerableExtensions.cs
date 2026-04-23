using TestTrackingDiagrams.Reports;
using Xunit;
using Xunit.v3;

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
                    DisplayName = ScenarioTitleResolver.FormatFeatureName(scenariosForFeature.Key),
                    Endpoint = featureClass.Traits.SingleOrDefault(y => y.Key == EndpointAttribute.EndpointTraitKey).Value?.FirstOrDefault(),
                    Scenarios = scenariosForFeature
                        .DistinctBy(x => x.Test!.UniqueID)
                        .OrderByDescending(x => x.TestMethod!.Traits.ContainsKey(HappyPathAttribute.HappyPathTraitKey))
                        .ThenBy(x => x.Test?.TestDisplayName)
                        .Select(x =>
                        {
                            var displayName = ScenarioTitleResolver.FormatScenarioDisplayName(x.Test!.TestDisplayName);

                            // Try structured extraction from TestMethodArguments first
                            var structuredResult = TryExtractStructuredParametersWithRaw(x);
                            var structuredParams = structuredResult?.StringValues;
                            Dictionary<string, object?>? rawValues = structuredResult?.RawValues;
                            var parsed = structuredParams ?? ParameterParser.Parse(displayName);

                            return new Scenario
                            {
                                Id = x.Test!.UniqueID,
                                Result = x.TestState!.Result.ToExecutionResult(),
                                DisplayName = displayName,
                                IsHappyPath = x.Test!.Traits.ContainsKey(HappyPathAttribute.HappyPathTraitKey),
                                ErrorMessage = string.Join(Environment.NewLine, x.TestState!.FailureCause) + Environment.NewLine + string.Join(Environment.NewLine, x.TestState!.ExceptionMessages ?? []),
                                ErrorStackTrace = string.Join(Environment.NewLine, x.TestState!.ExceptionStackTraces ?? []),
                                Duration = x.TestState!.ExecutionTime is > 0 ? TimeSpan.FromMilliseconds((double)x.TestState.ExecutionTime.Value) : null,
                                OutlineId = parsed is { Count: > 0 } ? (structuredParams is not null ? GetStructuredOutlineId(x) : ParameterParser.ExtractBaseName(displayName)) : null,
                                ExampleValues = parsed is { Count: > 0 } ? parsed : null,
                                ExampleRawValues = rawValues
                            };
                        }).ToArray()
                };
            }).ToArray();
    }

    private static (Dictionary<string, string> StringValues, Dictionary<string, object?> RawValues)? TryExtractStructuredParametersWithRaw(ITestContext context)
    {
        try
        {
            if (context.TestMethod is not IXunitTestMethod xunitMethod)
                return null;

            var args = xunitMethod.TestMethodArguments;
            var parameters = xunitMethod.Parameters;
            if (args is not { Length: > 0 } || parameters is not { Count: > 0 })
                return null;

            var paramNames = parameters.Select(p => p.Name).ToArray();
            return ParameterParser.ExtractStructuredParametersWithRaw(args, paramNames);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetStructuredOutlineId(ITestContext context)
    {
        if (context.TestMethod is IXunitTestMethod xunitMethod)
            return xunitMethod.Method?.Name;
        return null;
    }
}