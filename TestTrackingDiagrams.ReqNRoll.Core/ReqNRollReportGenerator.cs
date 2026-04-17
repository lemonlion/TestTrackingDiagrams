using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.ReqNRoll;

public static class ReqNRollReportGenerator
{
    public static void CreateStandardReportsWithDiagrams(ReportConfigurationOptions options)
    {
        var scenarios = ReqNRollScenarioCollector.GetAll();
        var startRunTime = ReqNRollScenarioCollector.StartRunTime == default ? DateTime.UtcNow : ReqNRollScenarioCollector.StartRunTime;
        var endRunTime = ReqNRollScenarioCollector.EndRunTime == default ? DateTime.UtcNow : ReqNRollScenarioCollector.EndRunTime;
        CreateStandardReportsWithDiagrams(scenarios, startRunTime, endRunTime, options);
    }

    public static void CreateStandardReportsWithDiagrams(IEnumerable<ReqNRollScenarioInfo> scenarios, DateTime startRunTime, DateTime endRunTime, ReportConfigurationOptions options)
    {
        var features = scenarios.ToArray().ToFeatures();
        ReportGenerator.CreateStandardReportsWithDiagrams(features, startRunTime, endRunTime, options);
    }
}
