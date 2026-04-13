using LightBDD.Core.Results;
using LightBDD.Core.Results.Parameters;
using LightBDD.Core.Results.Parameters.Tabular;
using LightBDD.Core.Results.Parameters.Trees;
using LightBDD.Core.Metadata;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.LightBDD.TUnit;

internal static class FeatureResultExtensions
{
    public static Feature[] ToFeatures(this IEnumerable<IFeatureResult> featureResults)
    {
        return featureResults
            .OrderBy(f => f.Info.Name.ToString())
            .Select(f =>
            {
                var labels = f.Info.Labels.ToArray();
                return new Feature
                {
                    DisplayName = f.Info.Name.ToString(),
                    Description = f.Info.Description,
                    Labels = labels.Length > 0 ? labels : null,
                    Scenarios = f.GetScenarios()
                        .OrderBy(s => s.Info.Name.ToString())
                        .Select(MapScenario)
                        .ToArray()
                };
            }).ToArray();
    }

    private static Scenario MapScenario(IScenarioResult result)
    {
        var labels = result.Info.Labels.ToArray();
        var categories = result.Info.Categories.ToArray();

        return new Scenario
        {
            Id = result.Info.RuntimeId.ToString(),
            DisplayName = result.Info.Name.ToString(),
            Result = MapStatus(result.Status),
            ErrorMessage = result.Status == ExecutionStatus.Failed ? result.StatusDetails : null,
            Duration = result.ExecutionTime?.Duration,
            Steps = MapSteps(result.GetSteps(), result.Status is ExecutionStatus.Ignored or ExecutionStatus.NotRun),
            IsHappyPath = labels.Contains("Happy Path"),
            Labels = labels.Length > 0 ? labels : null,
            Categories = categories.Length > 0 ? categories : null,
        };
    }

    private static ScenarioStep[]? MapSteps(IEnumerable<IStepResult> steps, bool scenarioSkipped = false)
    {
        var stepArray = steps.ToArray();
        if (stepArray.Length == 0) return null;
        var mapped = new ScenarioStep[stepArray.Length];
        var priorFailure = false;
        for (var i = 0; i < stepArray.Length; i++)
        {
            mapped[i] = MapStep(stepArray[i], priorFailure, scenarioSkipped);
            if (stepArray[i].Status == ExecutionStatus.Failed)
                priorFailure = true;
        }
        return mapped;
    }

    private static ScenarioStep MapStep(IStepResult step, bool priorFailure, bool scenarioSkipped)
    {
        var keyword = step.Info.Name.StepTypeName?.OriginalName;
        var text = step.Info.Name.ToString();

        // Strip the keyword prefix from the full text if it appears (LightBDD includes it)
        if (keyword != null && text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
        {
            text = text[keyword.Length..].TrimStart();
        }

        var comments = step.Comments?.ToArray();
        var attachments = step.FileAttachments?
            .Select(a => new Reports.FileAttachment(a.Name, a.RelativePath))
            .ToArray();
        var parameters = step.Parameters?.Count > 0
            ? step.Parameters.Select(MapParameter).ToArray()
            : null;

        return new ScenarioStep
        {
            Keyword = keyword,
            Text = text,
            Status = MapStepStatus(step.Status, priorFailure, scenarioSkipped),
            Duration = step.ExecutionTime?.Duration,
            SubSteps = MapSteps(step.GetSubSteps(), scenarioSkipped),
            Comments = comments is { Length: > 0 } ? comments : null,
            Attachments = attachments is { Length: > 0 } ? attachments : null,
            Parameters = parameters,
        };
    }

    private static ExecutionResult MapStatus(ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Passed => ExecutionResult.Passed,
            ExecutionStatus.Failed => ExecutionResult.Failed,
            ExecutionStatus.Bypassed => ExecutionResult.Bypassed,
            ExecutionStatus.Ignored => ExecutionResult.Skipped,
            ExecutionStatus.NotRun => ExecutionResult.Skipped,
            _ => ExecutionResult.Failed
        };
    }

    private static ExecutionResult MapStepStatus(ExecutionStatus status, bool priorFailure, bool scenarioSkipped)
    {
        return status switch
        {
            ExecutionStatus.Passed => ExecutionResult.Passed,
            ExecutionStatus.Failed => ExecutionResult.Failed,
            ExecutionStatus.Bypassed => ExecutionResult.Bypassed,
            ExecutionStatus.Ignored => scenarioSkipped || !priorFailure ? ExecutionResult.Skipped : ExecutionResult.SkippedAfterFailure,
            ExecutionStatus.NotRun => scenarioSkipped ? ExecutionResult.Skipped : ExecutionResult.SkippedAfterFailure,
            _ => ExecutionResult.Failed
        };
    }

    private static StepParameter MapParameter(IParameterResult param)
    {
        return param.Details switch
        {
            IInlineParameterDetails inline => new StepParameter
            {
                Name = param.Name,
                Kind = StepParameterKind.Inline,
                InlineValue = new InlineParameterValue(
                    inline.Value,
                    inline.Expectation,
                    MapVerificationStatus(inline.VerificationStatus))
            },
            ITabularParameterDetails table => new StepParameter
            {
                Name = param.Name,
                Kind = StepParameterKind.Tabular,
                TabularValue = new TabularParameterValue(
                    table.Columns.Select(c => new TabularColumn(c.Name, c.IsKey)).ToArray(),
                    table.Rows.Select(r => new TabularRow(
                        MapRowType(r.Type),
                        r.Values.Select(v => new TabularCell(v.Value, v.Expectation, MapVerificationStatus(v.VerificationStatus))).ToArray()
                    )).ToArray())
            },
            ITreeParameterDetails tree => new StepParameter
            {
                Name = param.Name,
                Kind = StepParameterKind.Tree,
                TreeValue = new TreeParameterValue(MapTreeNode(tree.Root))
            },
            _ => new StepParameter
            {
                Name = param.Name,
                Kind = StepParameterKind.Inline,
                InlineValue = new InlineParameterValue(param.Details?.ToString() ?? "", null, Reports.VerificationStatus.NotApplicable)
            }
        };
    }

    private static Reports.TreeNode MapTreeNode(ITreeParameterNodeResult node)
    {
        return new Reports.TreeNode(
            node.Path,
            node.Node,
            node.Value,
            node.Expectation,
            MapVerificationStatus(node.VerificationStatus),
            node.Children?.Count > 0
                ? node.Children.Select(MapTreeNode).ToArray()
                : null);
    }

    private static Reports.VerificationStatus MapVerificationStatus(ParameterVerificationStatus status)
    {
        return status switch
        {
            ParameterVerificationStatus.NotApplicable => Reports.VerificationStatus.NotApplicable,
            ParameterVerificationStatus.Success => Reports.VerificationStatus.Success,
            ParameterVerificationStatus.Failure => Reports.VerificationStatus.Failure,
            ParameterVerificationStatus.Exception => Reports.VerificationStatus.Exception,
            ParameterVerificationStatus.NotProvided => Reports.VerificationStatus.NotProvided,
            _ => Reports.VerificationStatus.NotApplicable
        };
    }

    private static Reports.TableRowType MapRowType(global::LightBDD.Core.Results.Parameters.Tabular.TableRowType type)
    {
        return type switch
        {
            global::LightBDD.Core.Results.Parameters.Tabular.TableRowType.Matching => Reports.TableRowType.Matching,
            global::LightBDD.Core.Results.Parameters.Tabular.TableRowType.Surplus => Reports.TableRowType.Surplus,
            global::LightBDD.Core.Results.Parameters.Tabular.TableRowType.Missing => Reports.TableRowType.Missing,
            _ => Reports.TableRowType.Matching
        };
    }
}
