using Kronikol.Reports;
using Xunit;

namespace Kronikol.xUnit3;

/// <summary>
/// Generates test tracking reports from xUnit v3 test execution contexts.
/// </summary>
public static class XUnitReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(IEnumerable<ITestContext> testContexts, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        ReportGenerator.CreateStandardReportsWithDiagrams(testContexts.ToFeatures(), startRunTime, endRunTime, options);
    }
}