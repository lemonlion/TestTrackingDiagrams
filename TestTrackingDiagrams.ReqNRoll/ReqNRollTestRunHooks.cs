using Reqnroll;

namespace TestTrackingDiagrams.ReqNRoll;

[Binding]
public class ReqNRollTestRunHooks
{
    [BeforeTestRun(Order = int.MinValue)]
    public static void BeforeTestRun()
    {
        ReqNRollScenarioCollector.StartRunTime = DateTime.UtcNow;
    }

    [AfterTestRun(Order = int.MaxValue)]
    public static void AfterTestRun()
    {
        ReqNRollScenarioCollector.EndRunTime = DateTime.UtcNow;
    }
}
