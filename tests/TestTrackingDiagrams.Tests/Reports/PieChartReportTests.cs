using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class PieChartReportTests
{
    private static string GenerateReport(Feature[] features, bool includeTestRunData = true)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PieChart.html", "Test", includeTestRunData,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Report_contains_svg_pie_chart()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed },
                    new Scenario { Id = "s2", DisplayName = "S2", Result = ExecutionResult.Failed, ErrorMessage = "err" }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("<svg", content);
        Assert.Contains("summary-chart", content);
    }

    [Fact]
    public void Pie_chart_shows_pass_rate_percentage()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios = Enumerable.Range(1, 8).Select(i => new Scenario { Id = $"p{i}", DisplayName = $"P{i}", Result = ExecutionResult.Passed })
                    .Concat([
                        new Scenario { Id = "f1", DisplayName = "F1", Result = ExecutionResult.Failed, ErrorMessage = "err" },
                        new Scenario { Id = "f2", DisplayName = "F2", Result = ExecutionResult.Failed, ErrorMessage = "err" }
                    ]).ToArray()
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("80%", content);
    }

    [Fact]
    public void Pie_chart_omits_zero_count_segments()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed },
                    new Scenario { Id = "s2", DisplayName = "S2", Result = ExecutionResult.Passed }
                ]
            }
        };

        var content = GenerateReport(features);
        // The SVG pie chart should have only a green segment
        var svgStart = content.IndexOf("<div class=\"summary-chart\"");
        var svgEnd = content.IndexOf("</div>", svgStart);
        var svgContent = content[svgStart..svgEnd];
        Assert.Contains("#1daf26", svgContent); // green for passed
        Assert.DoesNotContain("#cc0000", svgContent); // no red for failed
    }

    [Fact]
    public void Pie_chart_not_shown_in_specifications()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios = [new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed }]
            }
        };

        var content = GenerateReport(features, includeTestRunData: false);
        Assert.DoesNotContain("<div class=\"summary-chart\"", content);
    }

    [Fact]
    public void Pie_chart_shows_correct_colors()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed },
                    new Scenario { Id = "s2", DisplayName = "S2", Result = ExecutionResult.Failed, ErrorMessage = "err" },
                    new Scenario { Id = "s3", DisplayName = "S3", Result = ExecutionResult.Skipped },
                    new Scenario { Id = "s4", DisplayName = "S4", Result = ExecutionResult.Bypassed }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("#1daf26", content); // green
        Assert.Contains("#cc0000", content); // red
        Assert.Contains("#949494", content); // gray
        Assert.Contains("#2e7bff", content); // blue
    }
}
