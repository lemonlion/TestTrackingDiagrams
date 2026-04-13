using Reqnroll;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.ReqNRoll.xUnit3;

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
                    Description = firstScenario.FeatureDescription,
                    Scenarios = featureGroup
                        .DistinctBy(x => x.ScenarioId)
                        .OrderByDescending(x => x.ScenarioTags.Contains(ReqNRollConstants.HappyPathTag, StringComparer.OrdinalIgnoreCase))
                        .ThenBy(x => x.ScenarioTitle)
                        .Select(x =>
                        {
                            var labels = x.ScenarioTags
                                .Where(t => !t.Equals(ReqNRollConstants.HappyPathTag, StringComparison.OrdinalIgnoreCase)
                                         && !t.StartsWith(ReqNRollConstants.EndpointTagPrefix, StringComparison.OrdinalIgnoreCase))
                                .ToArray();

                            return new Scenario
                            {
                                Id = x.ScenarioId,
                                DisplayName = x.ScenarioTitle,
                                IsHappyPath = x.ScenarioTags.Contains(ReqNRollConstants.HappyPathTag, StringComparer.OrdinalIgnoreCase),
                                Result = x.ExecutionStatus.ToExecutionResult(),
                                ErrorMessage = x.TestError?.Message,
                                ErrorStackTrace = x.TestError?.StackTrace,                                  Duration = x.Duration,                                Steps = x.Steps.Count > 0
                                    ? MapSteps(x.Steps)
                                    : null,
                                Labels = labels.Length > 0 ? labels : null,
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
            };
            if (steps[i].Status == ScenarioExecutionStatus.TestError || steps[i].Status == ScenarioExecutionStatus.BindingError)
                priorFailure = true;
        }
        return mapped;
    }
}
