using Reqnroll;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.ReqNRoll.TUnit;

public static class ExecutionStatusExtensions
{
    public static ExecutionResult ToExecutionResult(this ScenarioExecutionStatus status)
    {
        return status switch
        {
            ScenarioExecutionStatus.OK => ExecutionResult.Passed,
            ScenarioExecutionStatus.TestError => ExecutionResult.Failed,
            ScenarioExecutionStatus.BindingError => ExecutionResult.Failed,
            ScenarioExecutionStatus.UndefinedStep => ExecutionResult.Skipped,
            ScenarioExecutionStatus.StepDefinitionPending => ExecutionResult.Skipped,
            ScenarioExecutionStatus.Skipped => ExecutionResult.Skipped,
            _ => ExecutionResult.Failed
        };
    }

    public static ExecutionResult ToStepResult(this ScenarioExecutionStatus status, bool priorFailure)
    {
        return status switch
        {
            ScenarioExecutionStatus.OK => ExecutionResult.Passed,
            ScenarioExecutionStatus.TestError => ExecutionResult.Failed,
            ScenarioExecutionStatus.BindingError => ExecutionResult.Failed,
            ScenarioExecutionStatus.Skipped => priorFailure ? ExecutionResult.SkippedAfterFailure : ExecutionResult.Skipped,
            ScenarioExecutionStatus.UndefinedStep => ExecutionResult.Skipped,
            ScenarioExecutionStatus.StepDefinitionPending => ExecutionResult.Skipped,
            _ => ExecutionResult.Failed
        };
    }
}

