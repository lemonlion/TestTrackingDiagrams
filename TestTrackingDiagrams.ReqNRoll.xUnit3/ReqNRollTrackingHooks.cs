using Reqnroll;

namespace TestTrackingDiagrams.ReqNRoll.xUnit3;

[Binding]
public class ReqNRollTrackingHooks
{
    private readonly ScenarioContext _scenarioContext;
    private readonly FeatureContext _featureContext;

    public ReqNRollTrackingHooks(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        _scenarioContext = scenarioContext;
        _featureContext = featureContext;
    }

    [BeforeScenario(Order = int.MinValue)]
    public void BeforeScenario()
    {
        var scenarioId = Guid.NewGuid().ToString();
        _scenarioContext[ReqNRollConstants.ScenarioRuntimeIdKey] = scenarioId;
        _scenarioContext[ReqNRollConstants.StepsCollectionKey] = new List<ReqNRollStepInfo>();
        ReqNRollTestContext.CurrentTestInfo = (_scenarioContext.ScenarioInfo.Title, scenarioId);
    }

    [BeforeStep(Order = int.MinValue)]
    public void BeforeStep()
    {
        ReqNRollTestContext.CurrentStepType = _scenarioContext.StepContext.StepInfo.StepInstance.StepDefinitionKeyword.ToString();
    }

    [AfterStep(Order = int.MaxValue)]
    public void AfterStep()
    {
        var stepContext = _scenarioContext.StepContext;
        var steps = (List<ReqNRollStepInfo>)_scenarioContext[ReqNRollConstants.StepsCollectionKey];
        steps.Add(new ReqNRollStepInfo(stepContext.StepInfo.StepInstance.StepDefinitionKeyword.ToString(), stepContext.StepInfo.Text));
    }

    [AfterScenario(Order = int.MaxValue)]
    public void AfterScenario()
    {
        var scenarioId = (string)_scenarioContext[ReqNRollConstants.ScenarioRuntimeIdKey];
        var steps = (List<ReqNRollStepInfo>)_scenarioContext[ReqNRollConstants.StepsCollectionKey];

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
            Steps = steps
        });

        ReqNRollTestContext.CurrentTestInfo = null;
    }
}
