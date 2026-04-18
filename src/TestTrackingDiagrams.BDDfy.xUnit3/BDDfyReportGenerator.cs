using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public static class BDDfyReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(ReportConfigurationOptions options)
    {
        var scenarios = BDDfyScenarioCollector.GetAll();
        var startRunTime = BDDfyScenarioCollector.StartRunTime == default ? DateTime.UtcNow : BDDfyScenarioCollector.StartRunTime;
        var endRunTime = BDDfyScenarioCollector.EndRunTime == default ? DateTime.UtcNow : BDDfyScenarioCollector.EndRunTime;
        CreateStandardReportsWithDiagrams(scenarios, startRunTime, endRunTime, options);
    }

    public static void CreateStandardReportsWithDiagrams(IEnumerable<BDDfyScenarioInfo> scenarios, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        var features = scenarios.ToArray().ToFeatures();
        ReportGenerator.CreateStandardReportsWithDiagrams(features, startRunTime, endRunTime, options);
    }
}
