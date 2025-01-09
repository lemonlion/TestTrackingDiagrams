using TestTrackingDiagrams.Reports;
using NUnit.Framework.Interfaces;

namespace TestTrackingDiagrams.NUnit;

public static class TestStatusExtensions
{
    public static ScenarioResult ToScenarioResult(this TestStatus result)
    {
        return result switch
        {
            TestStatus.Passed => ScenarioResult.Passed,
            TestStatus.Warning => ScenarioResult.Passed,
            TestStatus.Failed => ScenarioResult.Failed,
            TestStatus.Skipped => ScenarioResult.Skipped,
            TestStatus.Inconclusive => ScenarioResult.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };
    }
}
