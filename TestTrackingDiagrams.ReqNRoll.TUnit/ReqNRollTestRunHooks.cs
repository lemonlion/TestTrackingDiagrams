using Reqnroll;

namespace TestTrackingDiagrams.ReqNRoll.TUnit;

[Binding]
public class ReqNRollTestRunHooks
{
    [BeforeTestRun(Order = int.MinValue)]
    public static void BeforeTestRun()
    {
        ReqNRollScenarioCollector.StartRunTime = DateTime.UtcNow;
    }

    [AfterTestRun(Order = int.MinValue)]
    public static void AfterTestRun()
    {
        ReqNRollScenarioCollector.EndRunTime = DateTime.UtcNow;
    }
}

