using System.Reflection;
using LightBDD.Core.Results;
using LightBDD.Framework.Reporting.Formatters;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.LightBDD;

/// <summary>
/// Report formatter that converts LightBDD results into the unified TTD Feature/Scenario model
/// and generates specifications data through ReportGenerator.
/// </summary>
public class UnifiedSpecificationsDataFormatter : IReportFormatter
{
    public string Title { get; set; } = "Specifications";
    public bool GenerateBlankOnFailedTests { get; set; } = true;
    public Func<int>? ExpectedTestCount { get; set; }
    public Func<DefaultDiagramsFetcher.DiagramAsCode[]>? DiagramsFetcher { get; set; }
    public DataFormat DataFormat { get; set; } = DataFormat.Yaml;

    public void Format(Stream stream, params IFeatureResult[] features)
    {
        if (ExpectedTestCount != null)
        {
            var scenarioCount = features.SelectMany(f => f.GetScenarios()).Count();
            if (scenarioCount != ExpectedTestCount())
                return;
        }

        var ttdFeatures = features.ToFeatures();

        var tempFileName = $"_unified_specs_{Guid.NewGuid():N}.{GetExtension(DataFormat)}";
        var path = ReportGenerator.GenerateSpecificationsData(
            ttdFeatures, tempFileName, Title, DataFormat, GenerateBlankOnFailedTests);

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
        _ => "yml"
    };
}
