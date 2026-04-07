using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.PlantUmlBrowser;

public class PlantUmlBrowserReportGeneratorTests
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
                    Result = ScenarioResult.Passed
                }
            ]
        }
    ];

    private static DefaultDiagramsFetcher.DiagramAsCode[] MakePlantUmlBrowserDiagrams(string testId = "test-1") =>
    [
        new DefaultDiagramsFetcher.DiagramAsCode(testId, "",
            "@startuml\nautonumber 1\nAlice -> Bob: Hello\n@enduml\n")
    ];

    [Fact]
    public void PlantUmlBrowser_report_contains_teavm_script_tags()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserScripts.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("cdn.jsdelivr.net/gh/lemonlion/plantuml-js-plantuml_limit_size_8192@v1.2026.3beta6-patched/plantuml.js", content);
        Assert.Contains("cdn.jsdelivr.net/gh/lemonlion/plantuml-js-plantuml_limit_size_8192@v1.2026.3beta6-patched/viz-global.js", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_contains_intersection_observer()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserObserver.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("IntersectionObserver", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_embeds_plantuml_in_data_attribute()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserData.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("data-plantuml-z=", content);
        Assert.Contains("plantuml-browser", content);
        // Diagram source is compressed, not in data-plantuml attribute
        Assert.DoesNotContain("data-plantuml=\"@startuml", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_does_not_contain_img_tags()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserNoImg.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.DoesNotContain("<img src=\"http", content);
        Assert.DoesNotContain("<img loading=", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_does_not_contain_mermaid()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserNoMermaid.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.DoesNotContain("<pre class=\"mermaid\"", content);
        Assert.DoesNotContain("mermaid.initialize", content);
        Assert.DoesNotContain("cdn.jsdelivr.net/npm/mermaid", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_shows_raw_plantuml_label()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserRawLabel.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("data-plantuml-z=", content);
        Assert.Contains("data-diagram-type=\"plantuml\"", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_assigns_unique_ids_per_diagram()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Feature",
                Scenarios =
                [
                    new Scenario { Id = "t1", DisplayName = "S1", IsHappyPath = true, Result = ScenarioResult.Passed },
                    new Scenario { Id = "t2", DisplayName = "S2", IsHappyPath = false, Result = ScenarioResult.Passed }
                ]
            }
        };

        var diagrams = new[]
        {
            new DefaultDiagramsFetcher.DiagramAsCode("t1", "", "@startuml\nA->B\n@enduml"),
            new DefaultDiagramsFetcher.DiagramAsCode("t2", "", "@startuml\nC->D\n@enduml")
        };

        var html = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserIds.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("id=\"puml-0\"", content);
        Assert.Contains("id=\"puml-1\"", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_html_encodes_special_characters_in_data_attribute()
    {
        var diagrams = new[]
        {
            new DefaultDiagramsFetcher.DiagramAsCode("test-1", "",
                "@startuml\nAlice -> Bob: <script>alert(\"xss\")</script> & stuff\n@enduml")
        };

        var html = ReportGenerator.GenerateHtmlReport(
            diagrams, MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserEncode.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("data-plantuml-z=\"", content);
        // Raw XSS payload should not appear in data-plantuml (it's compressed)
        Assert.DoesNotContain("data-plantuml=\"", content);
        // Unescaped script tags should never appear in HTML
        Assert.DoesNotContain("<script>alert", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_contains_loading_placeholder()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserPlaceholder.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("Loading diagram...", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_contains_DOMContentLoaded_wrapper()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserDCL.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("DOMContentLoaded", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_contains_plantumlLoad_call()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserLoad.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("plantumlLoad()", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_uses_window_plantuml_render()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserRender.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("window.plantuml.render", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_contains_rootMargin_200px()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserMargin.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("rootMargin: '200px'", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_contains_rendered_guard_and_unobserve()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserGuard.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("dataset.rendered", content);
        Assert.Contains("observer.unobserve(el)", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_does_not_contain_loading_lazy_attribute()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserNoLazy.html", "Test", true,
            lazyLoadImages: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.DoesNotContain("loading=\"lazy\"", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_sequential_ids_across_multiple_diagrams_per_scenario()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Feature",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "Scenario 1", IsHappyPath = true, Result = ScenarioResult.Passed }
                ]
            }
        };

        var diagrams = new[]
        {
            new DefaultDiagramsFetcher.DiagramAsCode("s1", "", "@startuml\nA->B\n@enduml"),
            new DefaultDiagramsFetcher.DiagramAsCode("s1", "", "@startuml\nC->D\n@enduml"),
            new DefaultDiagramsFetcher.DiagramAsCode("s1", "", "@startuml\nE->F\n@enduml")
        };

        var html = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserMulti.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.Contains("id=\"puml-0\"", content);
        Assert.Contains("id=\"puml-1\"", content);
        Assert.Contains("id=\"puml-2\"", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_no_diagrams_does_not_emit_diagrams_section()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            Array.Empty<DefaultDiagramsFetcher.DiagramAsCode>(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserNoDiagrams.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        Assert.DoesNotContain("data-plantuml-z=", content);
        Assert.DoesNotContain("data-plantuml=", content);
        Assert.DoesNotContain("id=\"puml-", content);
        Assert.DoesNotContain("Sequence Diagrams", content);
    }

    [Fact]
    public void PlantUmlBrowser_report_newlines_survive_in_data_attribute()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakePlantUmlBrowserDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "PlantUmlBrowserNewlines.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        var content = File.ReadAllText(html);
        // Diagram source is now gzip+base64 compressed in data-plantuml-z
        var dataAttrStart = content.IndexOf("data-plantuml-z=\"", StringComparison.Ordinal);
        Assert.True(dataAttrStart >= 0);
        var afterAttr = content.Substring(dataAttrStart + "data-plantuml-z=\"".Length);
        var attrEnd = afterAttr.IndexOf("\"", StringComparison.Ordinal);
        var compressed = afterAttr[..attrEnd];
        // Verify it's valid base64
        var bytes = Convert.FromBase64String(compressed);
        Assert.True(bytes.Length > 0);
        // Decompress and verify content
        using var input = new System.IO.Compression.GZipStream(new System.IO.MemoryStream(bytes), System.IO.Compression.CompressionMode.Decompress);
        using var reader = new System.IO.StreamReader(input);
        var decompressed = reader.ReadToEnd();
        Assert.Contains("@startuml", decompressed);
        Assert.Contains("@enduml", decompressed);
    }
}
