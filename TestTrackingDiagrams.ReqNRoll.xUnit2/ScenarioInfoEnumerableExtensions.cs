using Reqnroll;
using TestTrackingDiagrams;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.ReqNRoll.xUnit2;

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

                var featureLabels = firstScenario.CombinedTags
                    .Where(t => !t.Equals(ReqNRollConstants.HappyPathTag, StringComparison.OrdinalIgnoreCase)
                             && !t.StartsWith(ReqNRollConstants.EndpointTagPrefix, StringComparison.OrdinalIgnoreCase)
                             && !t.StartsWith(ReqNRollConstants.CategoryTagPrefix, StringComparison.OrdinalIgnoreCase)
                             && !firstScenario.ScenarioTags.Contains(t, StringComparer.OrdinalIgnoreCase))
                    .ToArray();

                return new Feature
                {
                    DisplayName = featureGroup.Key.Titleize(),
                    Endpoint = endpoint,
                    Description = firstScenario.FeatureDescription,
                    Labels = featureLabels.Length > 0 ? featureLabels : null,
                    Scenarios = featureGroup
                        .DistinctBy(x => x.ScenarioId)
                        .OrderByDescending(x => x.ScenarioTags.Contains(ReqNRollConstants.HappyPathTag, StringComparer.OrdinalIgnoreCase))
                        .ThenBy(x => x.ScenarioTitle)
                        .Select(x =>
                        {
                            var labels = x.ScenarioTags
                                .Where(t => !t.Equals(ReqNRollConstants.HappyPathTag, StringComparison.OrdinalIgnoreCase)
                                         && !t.StartsWith(ReqNRollConstants.EndpointTagPrefix, StringComparison.OrdinalIgnoreCase)
                                         && !t.StartsWith(ReqNRollConstants.CategoryTagPrefix, StringComparison.OrdinalIgnoreCase))
                                .ToArray();

                            var categories = x.CombinedTags
                                .Where(t => t.StartsWith(ReqNRollConstants.CategoryTagPrefix, StringComparison.OrdinalIgnoreCase))
                                .Select(t => t[ReqNRollConstants.CategoryTagPrefix.Length..])
                                .ToArray();

                            return new Scenario
                            {
                                Id = x.ScenarioId,
                                DisplayName = x.ScenarioTitle,
                                IsHappyPath = x.ScenarioTags.Contains(ReqNRollConstants.HappyPathTag, StringComparer.OrdinalIgnoreCase),
                                Result = x.ExecutionStatus.ToExecutionResult(),
                                ErrorMessage = x.TestError?.Message,
                                ErrorStackTrace = x.TestError?.StackTrace,
                                Duration = x.Duration,
                                Steps = x.Steps.Count > 0
                                    ? MapSteps(x.Steps)
                                    : null,
                                Labels = labels.Length > 0 ? labels : null,
                                Categories = categories.Length > 0 ? categories : null,
                                Rule = x.Rule,
                                OutlineId = x.OutlineId,
                                ExampleValues = x.ExampleValues,
                            };
                        }).ToArray()
                };
            }).ToArray();
    }

    private static ScenarioStep[] MapSteps(List<ReqNRollStepInfo> steps)
    {
        var mapped = new ScenarioStep[steps.Count];
        var priorFailure = false;
        for (var i = 0; i < steps.Count; i++)
        {
            mapped[i] = new ScenarioStep
            {
                Keyword = steps[i].Keyword,
                Text = steps[i].Text,
                Status = steps[i].Status.ToStepResult(priorFailure),
                Duration = steps[i].Duration,
                DocString = steps[i].DocString,
            };
            if (steps[i].Status == ScenarioExecutionStatus.TestError || steps[i].Status == ScenarioExecutionStatus.BindingError)
                priorFailure = true;
        }
        return mapped;
    }
}
