using LightBDD.Core.Results;
using LightBDD.Framework.Reporting.Formatters;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.LightBDD;

/// <summary>
/// A single LightBDD formatter that delegates all report generation to the standard
/// <see cref="ReportGenerator.CreateStandardReportsWithDiagrams"/> pipeline — the same
/// pipeline used by every other test-framework adapter (xUnit, NUnit, MSTest, TUnit, BDDfy, ReqNRoll).
/// This eliminates the need for separate formatters per report type and ensures feature parity
/// is maintained automatically as the core pipeline evolves.
/// </summary>
public class StandardPipelineFormatter : IReportFormatter
{
    public ReportConfigurationOptions Options { get; set; } = new();

    public void Format(Stream stream, params IFeatureResult[] features)
    {
        var ttdFeatures = features.ToFeatures();

        var startTime = features
            .SelectMany(f => f.GetScenarios())
            .Where(s => s.ExecutionTime != null)
            .Select(s => s.ExecutionTime!.Start.UtcDateTime)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Min();
        var endTime = features
            .SelectMany(f => f.GetScenarios())
            .Where(s => s.ExecutionTime != null)
            .Select(s => s.ExecutionTime!.End.UtcDateTime)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Max();

        ReportGenerator.CreateStandardReportsWithDiagrams(ttdFeatures, startTime, endTime, Options);
    }
}
