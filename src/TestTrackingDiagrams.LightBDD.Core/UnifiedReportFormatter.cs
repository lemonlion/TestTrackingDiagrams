using System.Reflection;
using LightBDD.Core.Results;
using LightBDD.Framework.Reporting.Formatters;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD;

/// <summary>
/// Report formatter that converts LightBDD results into the unified TTD Feature/Scenario model
/// and generates reports through ReportGenerator.
/// </summary>
public class UnifiedReportFormatter : IReportFormatter
{
    public string Title { get; set; } = "Test Run Report";
    public bool IncludeTestRunData { get; set; } = true;
    public Func<DefaultDiagramsFetcher.DiagramAsCode[]>? DiagramsFetcher { get; set; }
    public DiagramFormat DiagramFormat { get; set; } = DiagramFormat.PlantUml;
    public PlantUmlRendering PlantUmlRendering { get; set; } = PlantUmlRendering.BrowserJs;
    public bool LazyLoadImages { get; set; } = true;
    public string? Stylesheet { get; set; }
    public bool GenerateBlankOnFailedTests { get; set; }
    public Func<int>? ExpectedTestCount { get; set; }
    public bool InlineSvgRendering { get; set; }
    public bool InternalFlowTracking { get; set; }
    public string InternalFlowDataScript { get; set; } = "";
    public Dictionary<string, InternalFlowSegment>? WholeTestSegments { get; set; }
    public RequestResponseLog[]? TrackedLogs { get; set; }
    public WholeTestFlowVisualization WholeTestVisualization { get; set; } = WholeTestFlowVisualization.None;
    public CiMetadata? CiMetadata { get; set; }
    public bool ShowStepNumbers { get; set; }
    public string? CustomCss { get; set; }
    public string? CustomFaviconBase64 { get; set; }
    public string? CustomLogoHtml { get; set; }

    public void Format(Stream stream, params IFeatureResult[] features)
    {
        if (ExpectedTestCount != null)
        {
            var scenarioCount = features.SelectMany(f => f.GetScenarios()).Count();
            if (scenarioCount > ExpectedTestCount())
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

        // Use a temp file name for report generation, then copy the content to the stream
        var tempFileName = $"_unified_report_{Guid.NewGuid():N}.html";
        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, ttdFeatures, startTime, endTime, Stylesheet, tempFileName, Title, IncludeTestRunData,
            generateBlankOnFailedTests: GenerateBlankOnFailedTests,
            lazyLoadImages: LazyLoadImages, diagramFormat: DiagramFormat, plantUmlRendering: PlantUmlRendering,
            inlineSvgRendering: InlineSvgRendering, internalFlowTracking: InternalFlowTracking,
            internalFlowDataScript: InternalFlowDataScript, wholeTestSegments: WholeTestSegments,
            trackedLogs: TrackedLogs, wholeTestVisualization: WholeTestVisualization,
            ciMetadata: CiMetadata, showStepNumbers: ShowStepNumbers,
            customCss: CustomCss, customFaviconBase64: CustomFaviconBase64, customLogoHtml: CustomLogoHtml);

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
