using TestTrackingDiagrams.Reports;
using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

public static class TestStateExtensions
{
    public static ExecutionResult ToExecutionResult(this TestState state)
    {
        return state switch
        {
            TestState.Passed => ExecutionResult.Passed,
            TestState.Failed => ExecutionResult.Failed,
            TestState.Skipped => ExecutionResult.Skipped,
            TestState.Timeout => ExecutionResult.Failed,
            TestState.Cancelled => ExecutionResult.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }
}
