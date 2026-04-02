using TestTrackingDiagrams.Reports;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestTrackingDiagrams.MSTest;

public static class TestOutcomeExtensions
{
    public static ScenarioResult ToScenarioResult(this UnitTestOutcome outcome)
    {
        return outcome switch
        {
            UnitTestOutcome.Passed => ScenarioResult.Passed,
            UnitTestOutcome.Failed => ScenarioResult.Failed,
            UnitTestOutcome.Error => ScenarioResult.Failed,
            UnitTestOutcome.Timeout => ScenarioResult.Failed,
            UnitTestOutcome.Aborted => ScenarioResult.Failed,
            UnitTestOutcome.Inconclusive => ScenarioResult.Skipped,
            UnitTestOutcome.InProgress => ScenarioResult.Skipped,
            UnitTestOutcome.NotRunnable => ScenarioResult.Skipped,
            UnitTestOutcome.Unknown => ScenarioResult.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }
}
