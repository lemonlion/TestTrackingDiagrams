using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class VersionEmbeddingTests
{
    private static Feature[] SimpleFeatures() =>
    [
        new Feature
        {
            DisplayName = "F1",
            Scenarios = [new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed }]
        }
    ];

    [Fact]
    public void Html_report_contains_generator_meta_tag()
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], SimpleFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "VersionMeta.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        var content = File.ReadAllText(path);

        Assert.Contains("<meta name=\"generator\" content=\"TestTrackingDiagrams v", content);
    }

    [Fact]
    public void Html_report_contains_ttd_version_in_summary_table()
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], SimpleFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "VersionSummary.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        var content = File.ReadAllText(path);

        Assert.Contains("TTD Version:", content);
        Assert.Contains(ReportGenerator.TtdVersion, content);
    }

    [Fact]
    public void TtdVersion_is_not_null_or_empty()
    {
        Assert.NotNull(ReportGenerator.TtdVersion);
        Assert.NotEmpty(ReportGenerator.TtdVersion);
        Assert.NotEqual("unknown", ReportGenerator.TtdVersion);
    }
}
