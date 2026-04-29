using TestTrackingDiagrams.Reports;
using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

/// <summary>
/// Generates test tracking reports from TUnit test execution contexts.
/// </summary>
public static class TUnitReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(IEnumerable<TestContext> testContexts, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        ReportGenerator.CreateStandardReportsWithDiagrams(testContexts.ToFeatures(), startRunTime, endRunTime, options);
    }
}