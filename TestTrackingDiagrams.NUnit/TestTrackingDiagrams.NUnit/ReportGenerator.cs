using NUnit.Framework;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.NUnit;

public static class NUnitReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(IEnumerable<TestContext> testContexts, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        ReportGenerator.CreateStandardReportsWithDiagrams(testContexts.ToFeatures(), startRunTime, endRunTime, options);
    }
}