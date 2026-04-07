using LightBDD.Core.Results;
using LightBDD.Framework.Reporting.Formatters;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.LightBDD.xUnit2;

/// <summary>
/// Report formatter that converts LightBDD results into the unified TTD Feature/Scenario model
/// and generates reports through ReportGenerator.
/// </summary>
public class UnifiedReportFormatter : IReportFormatter
{
    public string Title { get; set; } = "Test Report";
    public bool IncludeTestRunData { get; set; } = true;
    public Func<DefaultDiagramsFetcher.DiagramAsCode[]>? DiagramsFetcher { get; set; }
    public DiagramFormat DiagramFormat { get; set; } = DiagramFormat.PlantUml;
    public PlantUmlRendering PlantUmlRendering { get; set; } = PlantUmlRendering.BrowserJs;
    public bool LazyLoadImages { get; set; } = true;

    public void Format(Stream stream, params IFeatureResult[] features)
    {
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

        // Use a temp file name for report generation, then copy the content to the stream
        var tempFileName = $"_unified_report_{Guid.NewGuid():N}.html";
        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, ttdFeatures, startTime, endTime, null, tempFileName, Title, IncludeTestRunData,
            lazyLoadImages: LazyLoadImages, diagramFormat: DiagramFormat, plantUmlRendering: PlantUmlRendering);

        try
        {
            using var fileStream = File.OpenRead(path);
            fileStream.CopyTo(stream);
        }
        finally
        {
            try { File.Delete(path); } catch { /* best effort cleanup */ }
        }
    }
}
