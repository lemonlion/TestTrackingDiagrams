using Kronikol;
using Kronikol.xUnit3;

namespace Kronikol.xUnit3.Infrastructure;

public class TestRun : DiagrammedTestRun, IDisposable
{
    public TestRun()
    {
        // Start any HTTP fakes here if needed
    }

    public void Dispose()
    {
        EndRunTime = DateTime.UtcNow;

        XUnitReportGenerator.CreateStandardReportsWithDiagrams(
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
