using LightBDD.Core.Results;
using LightBDD.Framework.Reporting.Formatters;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.LightBDD.xUnit3;

/// <summary>
/// Report formatter that converts LightBDD results into the unified TTD Feature/Scenario model
/// and generates YAML specifications through ReportGenerator.
/// </summary>
public class UnifiedYamlFormatter : IReportFormatter
{
    public string Title { get; set; } = "Specifications";
    public bool GenerateBlankOnFailedTests { get; set; } = true;
    public Func<DefaultDiagramsFetcher.DiagramAsCode[]>? DiagramsFetcher { get; set; }

    public void Format(Stream stream, params IFeatureResult[] features)
    {
        var ttdFeatures = features.ToFeatures();
        var diagrams = DiagramsFetcher?.Invoke() ?? [];

        var tempFileName = $"_unified_yaml_{Guid.NewGuid():N}.yml";
        var path = ReportGenerator.GenerateYamlSpecs(
            diagrams, ttdFeatures, tempFileName, Title, GenerateBlankOnFailedTests);

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
}
