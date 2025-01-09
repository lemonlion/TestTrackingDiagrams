using TestTrackingDiagrams.Reports;
using Xunit;

namespace TestTrackingDiagrams.XUnit;

public static class TestResultExtensions
{
    public static ScenarioResult ToScenarioResult(this TestResult result)
    {
        return result switch
        {
            TestResult.Passed => ScenarioResult.Passed,
            TestResult.Failed => ScenarioResult.Failed,
            TestResult.Skipped => ScenarioResult.Skipped,
            TestResult.NotRun => ScenarioResult.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };
    }
}
