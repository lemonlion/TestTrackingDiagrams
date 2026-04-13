using TestTrackingDiagrams.Reports;
using NUnit.Framework.Interfaces;

namespace TestTrackingDiagrams.NUnit4;

public static class TestStatusExtensions
{
    public static ExecutionResult ToExecutionResult(this TestStatus result)
    {
        return result switch
        {
            TestStatus.Passed => ExecutionResult.Passed,
            TestStatus.Warning => ExecutionResult.Passed,
            TestStatus.Failed => ExecutionResult.Failed,
            TestStatus.Skipped => ExecutionResult.Skipped,
            TestStatus.Inconclusive => ExecutionResult.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };
    }
}
