using TestTrackingDiagrams.Reports;
using Xunit;

namespace TestTrackingDiagrams.xUnit3;

/// <summary>
/// Provides extension methods for converting xUnit v3 <c>TestResult</c> values to <see cref="ExecutionResult"/>.
/// </summary>
public static class TestResultExtensions
{
    public static ExecutionResult ToExecutionResult(this TestResult result)
    {
        return result switch
        {
            TestResult.Passed => ExecutionResult.Passed,
            TestResult.Failed => ExecutionResult.Failed,
            TestResult.Skipped => ExecutionResult.Skipped,
            TestResult.NotRun => ExecutionResult.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };
    }
}