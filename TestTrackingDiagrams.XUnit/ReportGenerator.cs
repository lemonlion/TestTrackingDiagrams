using TestTrackingDiagrams.Reports;
using Xunit;

namespace TestTrackingDiagrams.XUnit;

public static class XUnitReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(IEnumerable<ITestContext> testContexts, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        ReportGenerator.CreateStandardReportsWithDiagrams(testContexts.ToFeatures(), startRunTime, endRunTime, options);
    }
}