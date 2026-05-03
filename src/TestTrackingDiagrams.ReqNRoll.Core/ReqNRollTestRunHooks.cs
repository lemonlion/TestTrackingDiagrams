using Reqnroll;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.ReqNRoll;

/// <summary>
/// Reqnroll binding hooks that track overall test run start and end times for report generation.
/// </summary>
[Binding]
public class ReqNRollTestRunHooks
{
    [BeforeTestRun(Order = int.MinValue)]
    public static void BeforeTestRun()
    {
        ReqNRollScenarioCollector.StartRunTime = DateTime.UtcNow;

        // Enable Track.That assertions to resolve the current ReqNRoll scenario ID.
        Track.TestIdResolver ??= () => ReqNRollTestContext.CurrentTestInfo?.Id;
    }

    [AfterTestRun(Order = int.MinValue)]
    public static void AfterTestRun()
    {
        ReqNRollScenarioCollector.EndRunTime = DateTime.UtcNow;
    }
}