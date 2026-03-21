using Reqnroll;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.ReqNRoll;

public static class ExecutionStatusExtensions
{
    public static ScenarioResult ToScenarioResult(this ScenarioExecutionStatus status)
    {
        return status switch
        {
            ScenarioExecutionStatus.OK => ScenarioResult.Passed,
            ScenarioExecutionStatus.TestError => ScenarioResult.Failed,
            ScenarioExecutionStatus.BindingError => ScenarioResult.Failed,
            ScenarioExecutionStatus.UndefinedStep => ScenarioResult.Skipped,
            ScenarioExecutionStatus.StepDefinitionPending => ScenarioResult.Skipped,
            ScenarioExecutionStatus.Skipped => ScenarioResult.Skipped,
            _ => ScenarioResult.Failed
        };
    }
}
