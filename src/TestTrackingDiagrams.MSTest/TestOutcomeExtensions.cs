using TestTrackingDiagrams.Reports;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestTrackingDiagrams.MSTest;

public static class TestOutcomeExtensions
{
    public static ExecutionResult ToExecutionResult(this UnitTestOutcome outcome)
    {
        return outcome switch
        {
            UnitTestOutcome.Passed => ExecutionResult.Passed,
            UnitTestOutcome.Failed => ExecutionResult.Failed,
            UnitTestOutcome.Error => ExecutionResult.Failed,
            UnitTestOutcome.Timeout => ExecutionResult.Failed,
            UnitTestOutcome.Aborted => ExecutionResult.Failed,
            UnitTestOutcome.Inconclusive => ExecutionResult.Skipped,
            UnitTestOutcome.InProgress => ExecutionResult.Skipped,
            UnitTestOutcome.NotRunnable => ExecutionResult.Skipped,
            UnitTestOutcome.Unknown => ExecutionResult.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }
}
