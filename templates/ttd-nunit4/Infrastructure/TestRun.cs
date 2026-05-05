using TestTrackingDiagrams;
using TestTrackingDiagrams.NUnit4;

// This lives OUTSIDE of a namespace so it applies assembly-wide
[SetUpFixture]
public class TestRun : DiagrammedTestRun
{
    [OneTimeSetUp]
    public static void GlobalSetup()
    {
        Setup();
    }

    [OneTimeTearDown]
    public static void GlobalTeardown()
    {
        EndRunTime = DateTime.UtcNow;

        NUnitReportGenerator.CreateStandardReportsWithDiagrams(
            TestContexts,
            StartRunTime,
            EndRunTime,
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "SERVICE_NAME Specifications",
                SeparateSetup = true,
            });
    }
}
