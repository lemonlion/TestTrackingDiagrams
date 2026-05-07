using TestTrackingDiagrams;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

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
                    DisplayName = featureGroup.Key.Titleize(),
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

                            // Prefer structured extraction from raw arguments (same mechanism as the
                            // non-BDDfy adapters) for rich sub-table rendering of complex objects.
                            // Fall back to string-based parsing of the scenario title.
                            Dictionary<string, string>? exampleValues = null;
                            Dictionary<string, object?>? exampleRawValues = null;
                            string? outlineId = null;

                            var structured = ParameterParser.ExtractStructuredParametersWithRaw(
                                x.RawArguments, x.ParameterNames);

                            if (structured is not null)
                            {
                                exampleValues = structured.Value.StringValues;
                                exampleRawValues = structured.Value.RawValues;
                                outlineId = ParameterParser.ExtractBaseName(x.ScenarioTitle);
                            }
                            else
                            {
                                var parsed = ParameterParser.Parse(x.ScenarioTitle);
                                if (parsed is { Count: > 0 })
                                {
                                    exampleValues = parsed;
                                    outlineId = ParameterParser.ExtractBaseName(x.ScenarioTitle);
                                }
                            }

                            return new Scenario
                            {
                                Id = x.TestId,
                                DisplayName = x.ScenarioTitle,
                                IsHappyPath = x.Tags.Contains(BDDfyConstants.HappyPathTag, StringComparer.OrdinalIgnoreCase),
                                Result = x.Steps.Count == 0 && x.Result == TestStack.BDDfy.Result.NotExecuted
                                    ? ExecutionResult.Passed
                                    : x.Result.ToExecutionResult(),
                                ErrorMessage = x.ErrorMessage,
                                ErrorStackTrace = x.ErrorStackTrace,
                                Duration = x.Duration != TimeSpan.Zero ? x.Duration : null,
                                Steps = x.Steps.Count > 0
                                    ? MapSteps(x.Steps)
                                    : StepCollector.GetSteps(x.TestId) is { Length: > 0 } collectedSteps ? collectedSteps : null,
                                Labels = labels.Length > 0 ? labels : null,
                                OutlineId = outlineId,
                                ExampleValues = exampleValues is { Count: > 0 } ? exampleValues : null,
                                ExampleRawValues = exampleRawValues,
                            };
                        }).ToArray())
                };
            }).ToArray();
    }

    private static ScenarioStep[] MapSteps(List<BDDfyStepInfo> steps)
    {
        var mapped = new ScenarioStep[steps.Count];
        var priorFailure = false;
        for (var i = 0; i < steps.Count; i++)
        {
            mapped[i] = new ScenarioStep
            {
                Keyword = steps[i].Keyword,
                Text = steps[i].Text,
                Status = steps[i].Result.ToStepResult(priorFailure),
                Duration = steps[i].Duration,
            };
            if (steps[i].Result == TestStack.BDDfy.Result.Failed)
                priorFailure = true;
        }
        return mapped;
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

            scenario.DisplayName = $"{scenario.DisplayName} [{index + 1}]";
        }

        return scenarios;
    }
}
