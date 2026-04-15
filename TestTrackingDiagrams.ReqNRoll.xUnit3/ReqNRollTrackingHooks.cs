using System.Diagnostics;
using Reqnroll;

namespace TestTrackingDiagrams.ReqNRoll.xUnit3;

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
        ReqNRollTestContext.CurrentStepType = _scenarioContext.StepContext.StepInfo.StepInstance.StepDefinitionKeyword.ToString();
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
        steps.Add(new ReqNRollStepInfo(
            stepContext.StepInfo.StepInstance.StepDefinitionKeyword.ToString(),
            stepContext.StepInfo.Text,
            stepContext.Status,
            stepStopwatch.Elapsed,
            tableText,
            docString));
    }

    [AfterScenario(Order = int.MaxValue)]
    public void AfterScenario()
    {
        _stopwatch?.Stop();
        var scenarioId = (string)_scenarioContext[ReqNRollConstants.ScenarioRuntimeIdKey];
        var steps = (List<ReqNRollStepInfo>)_scenarioContext[ReqNRollConstants.StepsCollectionKey];

        var arguments = _scenarioContext.ScenarioInfo.Arguments;
        Dictionary<string, string>? exampleValues = null;
        if (arguments is { Count: > 0 })
        {
            exampleValues = new Dictionary<string, string>();
            foreach (System.Collections.DictionaryEntry entry in arguments)
                exampleValues[entry.Key?.ToString() ?? ""] = entry.Value?.ToString() ?? "";
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
            ExampleValues = exampleValues
        });

        ReqNRollTestContext.CurrentTestInfo = null;
    }
}
