using Reqnroll;

namespace TestTrackingDiagrams.ReqNRoll;

[Binding]
/// <summary>
/// Reqnroll binding hooks that track overall test run start and end times for report generation.
/// </summary>
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