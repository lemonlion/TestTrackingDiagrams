using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class CiMetadataReportTests
{
    private static string GenerateReport(Feature[] features, CiMetadata? ciMetadata = null)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "CiMetadata.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs,
            ciMetadata: ciMetadata);
        return File.ReadAllText(path);
    }

    private static Feature[] SimpleFeatures() =>
    [
        new Feature
        {
            DisplayName = "F1",
            Scenarios = [new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed }]
        }
    ];

    [Fact]
    public void Report_shows_ci_metadata_when_present()
    {
        var ci = new CiMetadata(CiEnvironment.GitHubActions, "42", "main", "abc123def456789", null, "owner/repo", "12345");
        var content = GenerateReport(SimpleFeatures(), ci);
        Assert.Contains("Build #:", content);
        Assert.Contains("42", content);
        Assert.Contains("Branch:", content);
        Assert.Contains("main", content);
    }

    [Fact]
    public void Report_shows_pipeline_link()
    {
        var ci = new CiMetadata(CiEnvironment.GitHubActions, "42", "main", "abc123", "https://github.com/owner/repo/actions/runs/12345", "owner/repo", "12345");
        var content = GenerateReport(SimpleFeatures(), ci);
        Assert.Contains("<a href=\"https://github.com/owner/repo/actions/runs/12345\"", content);
        Assert.Contains("Pipeline", content);
    }

    [Fact]
    public void Report_shows_short_commit_sha_with_full_title()
    {
        var ci = new CiMetadata(CiEnvironment.GitHubActions, "42", "main", "abc123def456789", null, null, null);
        var content = GenerateReport(SimpleFeatures(), ci);
        Assert.Contains("abc123d", content); // 7-char display
        Assert.Contains("title=\"abc123def456789\"", content); // full SHA in title
    }

    [Fact]
    public void Report_omits_ci_section_when_null()
    {
        var content = GenerateReport(SimpleFeatures(), ciMetadata: null);
        Assert.DoesNotContain("Build #:", content);
        Assert.DoesNotContain("Pipeline", content);
    }
}
