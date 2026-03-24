using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.xUnit2;

public static class XUnit2ReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        var scenarios = XUnit2TestTrackingContext.GetAllScenarios();
        CreateStandardReportsWithDiagrams(scenarios, startRunTime, endRunTime, options);
    }

    public static void CreateStandardReportsWithDiagrams(IEnumerable<ScenarioInfo> scenarios, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        ReportGenerator.CreateStandardReportsWithDiagrams(scenarios.ToFeatures(), startRunTime, endRunTime, options);
    }
}
