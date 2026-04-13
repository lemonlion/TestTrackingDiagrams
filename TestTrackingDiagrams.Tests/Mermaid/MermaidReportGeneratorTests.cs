using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Mermaid;

public class MermaidReportGeneratorTests
{
    private static Feature[] MakeFeatures(string scenarioId = "test-1") =>
    [
        new Feature
        {
            DisplayName = "Order Feature",
            Scenarios =
            [
                new Scenario
                {
                    Id = scenarioId,
                    DisplayName = "Creates an order",
                    IsHappyPath = true,
                    Result = ExecutionResult.Passed
                }
            ]
        }
    ];

    private static DefaultDiagramsFetcher.DiagramAsCode[] MakeMermaidDiagrams(string testId = "test-1") =>
    [
        new DefaultDiagramsFetcher.DiagramAsCode(testId, "",
            "sequenceDiagram\nautonumber\nactor webApp as WebApp\nparticipant orderService as OrderService\nwebApp->>orderService: GET: /api/orders\norderService-->>webApp: OK\n")
    ];

    private static DefaultDiagramsFetcher.DiagramAsCode[] MakePlantUmlDiagrams(string testId = "test-1") =>
    [
        new DefaultDiagramsFetcher.DiagramAsCode(testId,
            "https://plantuml.com/plantuml/png/encoded123",
            "@startuml\nautonumber 1\n@enduml\n")
    ];

    [Fact]
    public void Mermaid_report_contains_pre_mermaid_tags()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakeMermaidDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "MermaidTest.html", "Test", true,
            diagramFormat: DiagramFormat.Mermaid);

        var content = File.ReadAllText(html);
        Assert.Contains("<pre class=\"mermaid\"", content);
    }

    [Fact]
    public void Mermaid_report_contains_mermaid_js_script()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakeMermaidDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "MermaidScriptTest.html", "Test", true,
            diagramFormat: DiagramFormat.Mermaid);

        var content = File.ReadAllText(html);
        Assert.Contains("mermaid.initialize", content);
        Assert.Contains("cdn.jsdelivr.net", content);
    }

    [Fact]
    public void Mermaid_report_does_not_contain_img_tags()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakeMermaidDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "MermaidNoImg.html", "Test", true,
            diagramFormat: DiagramFormat.Mermaid);

        var content = File.ReadAllText(html);
        Assert.DoesNotContain("<img src=\"http", content);
        Assert.DoesNotContain("<img loading=", content);
    }

    [Fact]
    public void Mermaid_report_contains_data_mermaid_source_attribute()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakeMermaidDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "MermaidRawLabel.html", "Test", true,
            diagramFormat: DiagramFormat.Mermaid);

        var content = File.ReadAllText(html);
        Assert.Contains("data-mermaid-source=", content);
        Assert.Contains("data-diagram-type=\"mermaid\"", content);
    }

    [Fact]
    public void PlantUml_report_still_contains_img_tags()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBackcompat.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.Server);

        var content = File.ReadAllText(html);
        Assert.Contains("<img", content);
        Assert.Contains("Raw Plant UML", content);
    }

    [Fact]
    public void Default_diagram_format_is_PlantUml()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "DefaultFormat.html", "Test", true);

        var content = File.ReadAllText(html);
        Assert.Contains("plantuml-browser", content);
        Assert.DoesNotContain("<pre class=\"mermaid\">", content);
    }

    [Fact]
    public void PlantUml_report_does_not_contain_plantuml_browser_scripts()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlNoBrowserScript.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.Server);

        var content = File.ReadAllText(html);
        Assert.DoesNotContain("plantuml-browser", content);
        Assert.DoesNotContain("viz-global.js", content);
        Assert.DoesNotContain("IntersectionObserver", content);
    }

    [Fact]
    public void PlantUml_report_does_not_contain_mermaid_script()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlNoMermaidScript.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml);

        var content = File.ReadAllText(html);
        Assert.DoesNotContain("mermaid.initialize", content);
    }

    [Fact]
    public void Mermaid_report_does_not_contain_plantuml_browser_scripts()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakeMermaidDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "MermaidNoBrowserScript.html", "Test", true,
            diagramFormat: DiagramFormat.Mermaid);

        var content = File.ReadAllText(html);
        Assert.DoesNotContain("<div class=\"plantuml-browser\"", content);
        Assert.DoesNotContain("viz-global.js", content);
        Assert.DoesNotContain("IntersectionObserver", content);
    }

    [Fact]
    public void DiagramFormat_enum_has_exactly_two_values()
    {
        var values = Enum.GetValues<DiagramFormat>();

        Assert.Equal(2, values.Length);
        Assert.Contains(DiagramFormat.PlantUml, values);
        Assert.Contains(DiagramFormat.Mermaid, values);
    }

    [Fact]
    public void PlantUmlRendering_enum_has_exactly_four_values()
    {
        var values = Enum.GetValues<PlantUmlRendering>();

        Assert.Equal(4, values.Length);
        Assert.Contains(PlantUmlRendering.Server, values);
        Assert.Contains(PlantUmlRendering.BrowserJs, values);
        Assert.Contains(PlantUmlRendering.Local, values);
        Assert.Contains(PlantUmlRendering.NodeJs, values);
    }
}
