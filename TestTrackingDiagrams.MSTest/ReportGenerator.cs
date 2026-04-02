using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.MSTest;

public static class MSTestReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(IEnumerable<MSTestScenarioInfo> scenarioInfos, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        ReportGenerator.CreateStandardReportsWithDiagrams(scenarioInfos.ToFeatures(), startRunTime, endRunTime, options);
    }
}
