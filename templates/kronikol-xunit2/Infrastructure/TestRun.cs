using Kronikol;
using Kronikol.xUnit2;

namespace Kronikol.xUnit2.Infrastructure;

public class TestRun : DiagrammedTestRun, IDisposable
{
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
