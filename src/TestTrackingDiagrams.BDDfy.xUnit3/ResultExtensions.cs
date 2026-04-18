using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public static class ResultExtensions
{
    public static ExecutionResult ToExecutionResult(this TestStack.BDDfy.Result result)
    {
        return result switch
        {
            TestStack.BDDfy.Result.Passed => ExecutionResult.Passed,
            TestStack.BDDfy.Result.Failed => ExecutionResult.Failed,
            TestStack.BDDfy.Result.Inconclusive => ExecutionResult.Skipped,
            TestStack.BDDfy.Result.NotImplemented => ExecutionResult.Skipped,
            TestStack.BDDfy.Result.NotExecuted => ExecutionResult.Skipped,
            _ => ExecutionResult.Failed
        };
    }

    internal static ExecutionResult ToStepResult(this TestStack.BDDfy.Result result, bool priorFailure)
    {
        return result switch
        {
            TestStack.BDDfy.Result.Passed => ExecutionResult.Passed,
            TestStack.BDDfy.Result.Failed => ExecutionResult.Failed,
            TestStack.BDDfy.Result.Inconclusive => ExecutionResult.Skipped,
            TestStack.BDDfy.Result.NotImplemented => ExecutionResult.Skipped,
            TestStack.BDDfy.Result.NotExecuted => priorFailure ? ExecutionResult.SkippedAfterFailure : ExecutionResult.Skipped,
            _ => ExecutionResult.Failed
        };
    }
}
