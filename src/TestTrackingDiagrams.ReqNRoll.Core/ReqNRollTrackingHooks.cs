using System.Diagnostics;
using Reqnroll;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.ReqNRoll;

/// <summary>
/// Reqnroll binding hooks that collect scenario and step execution information for test tracking diagrams and reports.
/// </summary>
[Binding]
public class ReqNRollTrackingHooks
{
    private readonly ScenarioContext _scenarioContext;
    private readonly FeatureContext _featureContext;
    private Stopwatch? _stopwatch;

    public ReqNRollTrackingHooks(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        _scenarioContext = scenarioContext;
        _featureContext = featureContext;
    }

    [BeforeScenario(Order = int.MinValue)]
    public void BeforeScenario()
    {
        _stopwatch = Stopwatch.StartNew();
        var scenarioId = Guid.NewGuid().ToString();
        _scenarioContext[ReqNRollConstants.ScenarioRuntimeIdKey] = scenarioId;
        _scenarioContext[ReqNRollConstants.StepsCollectionKey] = new List<ReqNRollStepInfo>();
        ReqNRollTestContext.CurrentTestInfo = (_scenarioContext.ScenarioInfo.Title, scenarioId);
    }

    [BeforeStep(Order = int.MinValue)]
    public void BeforeStep()
    {
        var stepType = _scenarioContext.StepContext.StepInfo.StepInstance.StepDefinitionKeyword.ToString();
        ReqNRollTestContext.CurrentStepType = stepType;
        TestPhaseContext.Current = PhaseConfiguration.ResolvePhaseFromStepType(stepType);
        _scenarioContext[ReqNRollConstants.StepStopwatchKey] = Stopwatch.StartNew();
    }

    [AfterStep(Order = int.MaxValue)]
    public void AfterStep()
    {
        var stepStopwatch = (Stopwatch)_scenarioContext[ReqNRollConstants.StepStopwatchKey];
        stepStopwatch.Stop();
        var stepContext = _scenarioContext.StepContext;
        var steps = (List<ReqNRollStepInfo>)_scenarioContext[ReqNRollConstants.StepsCollectionKey];
        var tableText = stepContext.StepInfo.Table?.ToString();
        var docString = stepContext.StepInfo.MultilineText;

        // Capture inline parameter positions from binding match
        InlineParamCapture[]? inlineParams = null;
        var bindingMatch = stepContext.StepInfo.BindingMatch;
        if (bindingMatch is { Success: true, Arguments.Length: > 0 })
        {
            var stepText = stepContext.StepInfo.Text;
            var captures = new List<InlineParamCapture>();
            var methodParams = bindingMatch.StepBinding?.Method?.Parameters?.ToArray();
            for (var i = 0; i < bindingMatch.Arguments.Length; i++)
            {
                var arg = bindingMatch.Arguments[i];
                if (arg.StartOffset is not null && arg.Value is string strValue)
                {
                    var offset = arg.StartOffset.Value;
                    var length = strValue.Length;
                    if (offset >= 0 && offset + length <= stepText.Length)
                    {
                        string? name = null;
                        if (methodParams != null && i < methodParams.Length)
                            name = methodParams[i].ParameterName;
                        captures.Add(new InlineParamCapture(offset, length, strValue, name));
                    }
                }
            }
            if (captures.Count > 0)
                inlineParams = captures.ToArray();
        }

        steps.Add(new ReqNRollStepInfo(
            stepContext.StepInfo.StepInstance.StepDefinitionKeyword.ToString(),
            stepContext.StepInfo.Text,
            stepContext.Status,
            stepStopwatch.Elapsed,
            tableText,
            docString,
            inlineParams));
    }

    [AfterScenario(Order = int.MaxValue)]
    public void AfterScenario()
    {
        _stopwatch?.Stop();
        var scenarioId = (string)_scenarioContext[ReqNRollConstants.ScenarioRuntimeIdKey];
        var steps = (List<ReqNRollStepInfo>)_scenarioContext[ReqNRollConstants.StepsCollectionKey];

        var arguments = _scenarioContext.ScenarioInfo.Arguments;
        Dictionary<string, string>? exampleValues = null;
        Dictionary<string, object?>? exampleRawValues = null;
        if (arguments is { Count: > 0 })
        {
            var flatValues = new Dictionary<string, string>();
            foreach (System.Collections.DictionaryEntry entry in arguments)
                flatValues[entry.Key?.ToString() ?? ""] = entry.Value?.ToString() ?? "";

            // Build structured ExampleValues/ExampleRawValues by grouping flat columns
            // based on step table data
            (exampleValues, exampleRawValues) = ExampleValueGrouper.BuildStructured(flatValues, steps);
        }

        ReqNRollScenarioCollector.Collect(new ReqNRollScenarioInfo
        {
            ScenarioId = scenarioId,
            ScenarioTitle = _scenarioContext.ScenarioInfo.Title,
            FeatureTitle = _featureContext.FeatureInfo.Title,
            FeatureDescription = _featureContext.FeatureInfo.Description,
            ScenarioTags = _scenarioContext.ScenarioInfo.Tags,
            CombinedTags = _scenarioContext.ScenarioInfo.CombinedTags,
            TestError = _scenarioContext.TestError,
            ExecutionStatus = _scenarioContext.ScenarioExecutionStatus,
            Duration = _stopwatch?.Elapsed,
            Steps = steps,
            Rule = _scenarioContext.RuleInfo?.Title,
            OutlineId = exampleValues is not null ? _scenarioContext.ScenarioInfo.Title : null,
            ExampleValues = exampleValues,
            ExampleRawValues = exampleRawValues
        });

        ReqNRollTestContext.CurrentTestInfo = null;
    }
}