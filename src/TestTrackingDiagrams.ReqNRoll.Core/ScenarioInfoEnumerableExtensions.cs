using Reqnroll;
using TestTrackingDiagrams;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.ReqNRoll;

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
                        .ThenBy(x => x.ExampleValues is { Count: > 0 }
                            ? string.Join("|", x.ExampleValues.Values)
                            : "")
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
                                ExampleRawValues = x.ExampleRawValues,
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
                Parameters = ParseTableText(steps[i].TableText),
                TextSegments = BuildTextSegments(steps[i]),
            };
            if (steps[i].Status == ScenarioExecutionStatus.TestError || steps[i].Status == ScenarioExecutionStatus.BindingError)
                priorFailure = true;
        }
        return mapped;
    }

    private static StepTextSegment[]? BuildTextSegments(ReqNRollStepInfo step)
    {
        if (step.InlineParams is not { Length: > 0 })
            return null;

        var segments = new List<StepTextSegment>();
        var text = step.Text;
        var lastEnd = 0;

        // Sort by offset to process left-to-right
        var sortedParams = step.InlineParams.OrderBy(p => p.StartOffset).ToArray();

        foreach (var param in sortedParams)
        {
            // Add literal text before this parameter
            if (param.StartOffset > lastEnd)
            {
                segments.Add(StepTextSegment.Literal(text[lastEnd..param.StartOffset]));
            }

            var paramValue = new InlineParameterValue(
                param.Value,
                null,
                VerificationStatus.NotApplicable);
            segments.Add(StepTextSegment.Param(param.Name, paramValue));

            lastEnd = param.StartOffset + param.Length;
        }

        // Add remaining literal text
        if (lastEnd < text.Length)
        {
            segments.Add(StepTextSegment.Literal(text[lastEnd..]));
        }

        return segments.Count > 0 ? segments.ToArray() : null;
    }

    private static StepParameter[]? ParseTableText(string? tableText)
    {
        if (string.IsNullOrWhiteSpace(tableText))
            return null;

        var lines = tableText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) // Need at least header + one data row
            return null;

        var headers = ParseTableRow(lines[0]);
        if (headers.Length == 0)
            return null;

        var columns = headers.Select(h => new TabularColumn(h, false)).ToArray();
        var rows = new List<TabularRow>();

        for (var i = 1; i < lines.Length; i++)
        {
            var cells = ParseTableRow(lines[i]);
            if (cells.Length == 0)
                continue;

            // Pad or truncate to match header count
            var tabularCells = new TabularCell[columns.Length];
            for (var j = 0; j < columns.Length; j++)
            {
                var value = j < cells.Length ? cells[j] : "";
                tabularCells[j] = new TabularCell(value, null, VerificationStatus.NotApplicable);
            }
            rows.Add(new TabularRow(TableRowType.Matching, tabularCells));
        }

        if (rows.Count == 0)
            return null;

        return [new StepParameter
        {
            Name = "table",
            Kind = StepParameterKind.Tabular,
            TabularValue = new TabularParameterValue(columns, rows.ToArray())
        }];
    }

    private static string[] ParseTableRow(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith('|') || !trimmed.EndsWith('|'))
            return [];

        return trimmed[1..^1]
            .Split('|')
            .Select(cell => cell.Trim())
            .ToArray();
    }
}
