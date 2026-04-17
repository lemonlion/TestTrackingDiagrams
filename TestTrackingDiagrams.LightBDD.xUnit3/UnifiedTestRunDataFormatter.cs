using System.Reflection;
using LightBDD.Core.Results;
using LightBDD.Framework.Reporting.Formatters;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD.xUnit3;

public class UnifiedTestRunDataFormatter : IReportFormatter
{
    public Assembly? TestAssembly { get; set; }
    public Func<DefaultDiagramsFetcher.DiagramAsCode[]>? DiagramsFetcher { get; set; }
    public DataFormat DataFormat { get; set; } = DataFormat.Json;
    public RequestResponseLog[]? TrackedLogs { get; set; }

    public void Format(Stream stream, params IFeatureResult[] features)
    {
        if (TestAssembly != null)
        {
            var scenarioCount = features.SelectMany(f => f.GetScenarios()).Count();
            var totalTests = TestAssembly.CountNumberOfTestsInAssembly();
            if (scenarioCount != totalTests)
                return;
        }

        var ttdFeatures = features.ToFeatures();
        var diagrams = DiagramsFetcher?.Invoke() ?? [];

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

        var trackedLogs = TrackedLogs ?? RequestResponseLogger.RequestAndResponseLogs
            .Where(x => !(x?.TrackingIgnore ?? true))
            .ToArray();

        var tempFileName = $"_unified_data_{Guid.NewGuid():N}.{GetExtension(DataFormat)}";
        var path = ReportGenerator.GenerateTestRunReportData(
            ttdFeatures, startTime, endTime, tempFileName, DataFormat, diagrams, trackedLogs);

        try
        {
            if (File.Exists(path))
            {
                using var fileStream = File.OpenRead(path);
                fileStream.CopyTo(stream);
            }
        }
        finally
        {
            try { File.Delete(path); } catch { /* best effort cleanup */ }
        }
    }

    private static string GetExtension(DataFormat format) => format switch
    {
        DataFormat.Json => "json",
        DataFormat.Xml => "xml",
        DataFormat.Yaml => "yml",
        _ => "json"
    };
}
