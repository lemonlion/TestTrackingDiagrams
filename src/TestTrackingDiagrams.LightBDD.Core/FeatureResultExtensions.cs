using System.Text.RegularExpressions;
using LightBDD.Core.Results;
using LightBDD.Core.Results.Parameters;
using LightBDD.Core.Results.Parameters.Tabular;
using LightBDD.Core.Results.Parameters.Trees;
using LightBDD.Core.Metadata;
using TestTrackingDiagrams;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.LightBDD;

internal static class FeatureResultExtensions
{
    // Matches [paramName: "{N}"] — bracket-appended parameters
    private static readonly Regex BracketParamPattern = new(@"\s*\[(\w+):\s*""\{(\d+)\}""\]", RegexOptions.Compiled);
    // Matches Word "{N}" — inline parameters (case-insensitive match in method name)
    private static readonly Regex InlineParamPattern = new(@"(\w+)\s+""\{(\d+)\}""", RegexOptions.Compiled);
    // Matches standalone "{N}" — UPPER CASE replacement parameters
    private static readonly Regex StandaloneParamPattern = new(@"\s*""\{(\d+)\}""", RegexOptions.Compiled);
    private static readonly Regex MultipleSpacesPattern = new(@"\s+", RegexOptions.Compiled);
    public static Feature[] ToFeatures(this IEnumerable<IFeatureResult> featureResults)
    {
        return featureResults
            .OrderBy(f => f.Info.Name.ToString())
            .Select(f =>
            {
                var labels = f.Info.Labels.ToArray();
                return new Feature
                {
                    DisplayName = f.Info.Name.ToString().Titleize(),
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

        var failedException = result.Status == ExecutionStatus.Failed
            ? FindFirstFailedException(result.GetSteps())
            : null;

        var nameInfo = result.Info.Name;
        var nameParams = nameInfo.Parameters.ToArray();

        string displayName;
        Dictionary<string, string>? exampleValues;
        Dictionary<string, object?>? exampleRawValues = null;
        string? outlineId;

        if (nameParams.Length > 0)
        {
            // Use LightBDD's structured NameFormat to extract all parameters,
            // including those substituted inline into the scenario name
            displayName = StripNamespacesFromText(nameInfo.ToString());
            (outlineId, exampleValues) = ExtractScenarioParameters(nameInfo.NameFormat, nameParams);

            // Try to look up captured raw parameter values (before stripping, values are used as keys)
            exampleRawValues = TryGetCapturedRawValues(nameParams, exampleValues);

            // Strip namespaces from extracted parameter values
            foreach (var key in exampleValues.Keys.ToArray())
                exampleValues[key] = StripNamespacesFromText(exampleValues[key]);
        }
        else
        {
            displayName = StripNamespacesFromText(nameInfo.ToString());
            var parsed = ParameterParser.Parse(displayName);
            outlineId = parsed is { Count: > 0 } ? ParameterParser.ExtractBaseName(displayName) : null;
            exampleValues = parsed is { Count: > 0 } ? parsed : null;
        }

        return new Scenario
        {
            Id = result.Info.RuntimeId.ToString(),
            DisplayName = displayName,
            Result = MapStatus(result.Status),
            ErrorMessage = result.Status == ExecutionStatus.Failed ? result.StatusDetails : null,
            ErrorStackTrace = failedException?.StackTrace,
            Duration = result.ExecutionTime?.Duration,
            Steps = MapSteps(result.GetSteps(), result.Status is ExecutionStatus.Ignored or ExecutionStatus.NotRun),
            IsHappyPath = labels.Contains("Happy Path"),
            Labels = labels.Length > 0 ? labels : null,
            Categories = categories.Length > 0 ? categories : null,
            OutlineId = outlineId,
            ExampleValues = exampleValues is { Count: > 0 } ? exampleValues : null,
            ExampleRawValues = exampleRawValues,
        };
    }

    /// <summary>
    /// Attempts to look up captured raw argument values that were stored during test execution.
    /// Uses the formatted parameter values as a content-based key for matching.
    /// </summary>
    private static Dictionary<string, object?>? TryGetCapturedRawValues(
        INameParameterInfo[] nameParams, Dictionary<string, string> exampleValues)
    {
        try
        {
            // Strategy 1: Try values-only key (most reliable - avoids param name mismatch)
            var formattedValues = nameParams.Select(p => p.FormattedValue ?? "").ToArray();
            var valuesKey = CapturedScenarioArguments.BuildValuesOnlyKey(formattedValues);
            var captured = CapturedScenarioArguments.TryGet(valuesKey);

            if (captured is null)
            {
                // Strategy 2: Try full key with exampleValues keys as param names
                var paramNames = exampleValues.Keys.ToArray();
                if (paramNames.Length == formattedValues.Length)
                {
                    var fullKey = CapturedScenarioArguments.BuildKey(paramNames, formattedValues);
                    captured = CapturedScenarioArguments.TryGet(fullKey);
                }
            }

            if (captured is null)
                return null;

            // Build ExampleRawValues using the same parameter names as ExampleValues
            var rawValues = new Dictionary<string, object?>();
            var capturedParamNames = captured.Value.ParamNames;
            var capturedRawValues = captured.Value.RawValues;

            // Map captured raw values to exampleValues keys by matching parameter names
            foreach (var kvp in exampleValues)
            {
                var paramIdx = Array.FindIndex(capturedParamNames,
                    n => n.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                if (paramIdx >= 0 && paramIdx < capturedRawValues.Length)
                {
                    rawValues[kvp.Key] = capturedRawValues[paramIdx];
                }
            }

            return rawValues.Count > 0 ? rawValues : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts parameter names and values from LightBDD's NameFormat and INameParameterInfo[].
    /// LightBDD generates three parameter patterns in the NameFormat:
    /// 1. [paramName: "{N}"] — appended when the parameter name wasn't found in the method name
    /// 2. MatchedWord "{N}" — inserted after the word when parameter matched case-insensitively
    /// 3. "{N}" — replaces the word when parameter was UPPER CASE in the method name
    /// </summary>
    internal static (string BaseName, Dictionary<string, string> Parameters) ExtractScenarioParameters(
        string nameFormat, INameParameterInfo[] parameters)
    {
        var result = new Dictionary<string, string>();
        var cleanFormat = nameFormat;

        // 1. Extract bracket-appended params: [paramName: "{N}"]
        foreach (Match m in BracketParamPattern.Matches(nameFormat))
        {
            var paramName = m.Groups[1].Value;
            if (int.TryParse(m.Groups[2].Value, out var index) && index < parameters.Length)
                result[paramName] = parameters[index].FormattedValue ?? "";
        }
        cleanFormat = BracketParamPattern.Replace(cleanFormat, "");

        // 2. Extract inline params: Word "{N}" (word before is the humanized parameter name)
        foreach (Match m in InlineParamPattern.Matches(cleanFormat))
        {
            var precedingWord = m.Groups[1].Value;
            if (int.TryParse(m.Groups[2].Value, out var index) && index < parameters.Length)
                result.TryAdd(precedingWord, parameters[index].FormattedValue ?? "");
        }
        cleanFormat = InlineParamPattern.Replace(cleanFormat, "$1");

        // 3. Handle remaining standalone "{N}" (from UPPER CASE replacement in method name)
        var standaloneCount = 0;
        foreach (Match m in StandaloneParamPattern.Matches(cleanFormat))
        {
            if (int.TryParse(m.Groups[1].Value, out var index) && index < parameters.Length)
            {
                result.TryAdd($"param{standaloneCount}", parameters[index].FormattedValue ?? "");
                standaloneCount++;
            }
        }
        cleanFormat = StandaloneParamPattern.Replace(cleanFormat, "");

        // Clean up whitespace
        cleanFormat = MultipleSpacesPattern.Replace(cleanFormat, " ").Trim();

        return (cleanFormat, result);
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

        // Strip fully-qualified type names (e.g. "Namespace.TypeName" → "TypeName")
        text = StripNamespacesFromText(text);

        var comments = step.Comments?.ToArray();
        var attachments = step.FileAttachments?
            .Select(a => new Reports.FileAttachment(a.Name, a.RelativePath))
            .ToArray();
        var parameters = step.Parameters?.Count > 0
            ? step.Parameters.Select(MapParameter).ToArray()
            : null;

        var textSegments = BuildTextSegments(step, keyword);

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
            TextSegments = textSegments,
        };
    }

    // Matches "{N}" placeholders (with surrounding quotes) in NameFormat
    private static readonly Regex FormatPlaceholderPattern = new(@"""?\{(\d+)\}""?", RegexOptions.Compiled);

    private static StepTextSegment[]? BuildTextSegments(IStepResult step, string? keyword)
    {
        var nameParams = step.Info.Name.Parameters.ToArray();
        if (nameParams.Length == 0)
            return null;

        var nameFormat = step.Info.Name.NameFormat;

        // Strip keyword prefix from the format string (same as we do for Text)
        if (keyword != null && nameFormat.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
        {
            nameFormat = nameFormat[keyword.Length..].TrimStart();
        }

        // Strip bracket-appended params from format (they're not inline text params)
        nameFormat = BracketParamPattern.Replace(nameFormat, "");

        // Build a lookup from IStepResult.Parameters for expectations (inline params by position)
        var inlineExpectations = new Dictionary<int, string?>();
        var resultParams = step.Parameters?.ToArray();
        if (resultParams != null)
        {
            var inlineIndex = 0;
            foreach (var rp in resultParams)
            {
                if (rp.Details is IInlineParameterDetails inline)
                {
                    inlineExpectations[inlineIndex] = inline.Expectation;
                    inlineIndex++;
                }
            }
        }

        var segments = new List<StepTextSegment>();
        var lastEnd = 0;

        foreach (Match match in FormatPlaceholderPattern.Matches(nameFormat))
        {
            // Add literal text before this placeholder
            if (match.Index > lastEnd)
            {
                var literal = nameFormat[lastEnd..match.Index];
                literal = StripNamespacesFromText(literal);
                segments.Add(StepTextSegment.Literal(literal));
            }

            if (int.TryParse(match.Groups[1].Value, out var paramIndex) && paramIndex < nameParams.Length)
            {
                var nameParam = nameParams[paramIndex];
                var expectation = inlineExpectations.GetValueOrDefault(paramIndex);
                var paramValue = new InlineParameterValue(
                    nameParam.FormattedValue ?? "",
                    expectation,
                    MapVerificationStatus(nameParam.VerificationStatus));

                // Try to get param name from result parameters
                string? paramName = null;
                if (resultParams != null)
                {
                    var inlineResults = resultParams.Where(rp => rp.Details is IInlineParameterDetails).ToArray();
                    if (paramIndex < inlineResults.Length)
                        paramName = inlineResults[paramIndex].Name;
                }

                segments.Add(StepTextSegment.Param(paramName, paramValue));
            }

            lastEnd = match.Index + match.Length;
        }

        // Add remaining literal text after the last placeholder
        if (lastEnd < nameFormat.Length)
        {
            var literal = nameFormat[lastEnd..];
            literal = StripNamespacesFromText(literal);
            segments.Add(StepTextSegment.Literal(literal));
        }

        return segments.Count > 0 ? segments.ToArray() : null;
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

    private static Exception? FindFirstFailedException(IEnumerable<IStepResult> steps)
    {
        foreach (var step in steps)
        {
            if (step.Status == ExecutionStatus.Failed && step.ExecutionException != null)
                return step.ExecutionException;

            var subException = FindFirstFailedException(step.GetSubSteps());
            if (subException != null)
                return subException;
        }
        return null;
    }

    private static string StripNamespacesFromText(string text)
    {
        // Replace quoted fully-qualified type names like "Namespace.Sub.TypeName" with just "TypeName"
        return System.Text.RegularExpressions.Regex.Replace(
            text,
            @"""([A-Za-z_]\w*\.)+([A-Za-z_]\w*)""",
            @"""$2""");
    }
}
