using Kronikol.Reports;

namespace Kronikol.Tests.Reports;

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

        Assert.Contains("<meta name=\"generator\" content=\"Kronikol v", content);
    }

    [Fact]
    public void Html_report_contains_kronikol_version_in_summary_table()
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], SimpleFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "VersionSummary.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        var content = File.ReadAllText(path);

        Assert.Contains("Kronikol Version:", content);
        Assert.Contains(ReportGenerator.KronikolVersion, content);
    }

    [Fact]
    public void Html_report_kronikol_version_row_is_hidden()
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], SimpleFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "VersionHidden.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        var content = File.ReadAllText(path);

        // The Kronikol Version row should be hidden from the user
        Assert.Contains("style=\"display:none\"", content.Substring(content.IndexOf("Kronikol Version") - 50, 100));
    }

    [Fact]
    public void KronikolVersion_is_not_null_or_empty()
    {
        Assert.NotNull(ReportGenerator.KronikolVersion);
        Assert.NotEmpty(ReportGenerator.KronikolVersion);
        Assert.NotEqual("unknown", ReportGenerator.KronikolVersion);
    }
}
