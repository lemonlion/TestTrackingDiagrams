using NUnit.Framework;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.NUnit4;

/// <summary>
/// Generates test tracking reports from NUnit test execution contexts.
/// </summary>
public static class NUnitReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(IEnumerable<TestContext> testContexts, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        ReportGenerator.CreateStandardReportsWithDiagrams(testContexts.ToFeatures(), startRunTime, endRunTime, options);
    }
}