using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public static class ResultExtensions
{
    public static ScenarioResult ToScenarioResult(this TestStack.BDDfy.Result result)
    {
        return result switch
        {
            TestStack.BDDfy.Result.Passed => ScenarioResult.Passed,
            TestStack.BDDfy.Result.Failed => ScenarioResult.Failed,
            TestStack.BDDfy.Result.Inconclusive => ScenarioResult.Skipped,
            TestStack.BDDfy.Result.NotImplemented => ScenarioResult.Skipped,
            TestStack.BDDfy.Result.NotExecuted => ScenarioResult.Ignored,
            _ => ScenarioResult.Failed
        };
    }
}
