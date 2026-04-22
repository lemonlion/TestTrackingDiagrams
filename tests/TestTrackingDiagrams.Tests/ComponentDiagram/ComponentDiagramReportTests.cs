using System.Net;
using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.ComponentDiagram;

[Collection("DiagramsFetcher")]
public class ComponentDiagramReportTests : IDisposable
{
    private readonly string _reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

    public void Dispose()
    {
        // Cleanup generated test files
        foreach (var file in Directory.GetFiles(_reportDir, "ComponentDiagram*"))
            File.Delete(file);
    }

    private static RequestResponseLog MakeRequest(
        string testId = "test-1",
        string callerName = "Caller",
        string serviceName = "OrderService",
        string method = "GET",
        Guid? requestResponseId = null)
    {
        return new RequestResponseLog(
            TestName: "Test",
            TestId: testId,
            Method: HttpMethod.Parse(method),
            Content: null,
            Uri: new Uri("http://example.com/api"),
            Headers: [],
            ServiceName: serviceName,
            CallerName: callerName,
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: requestResponseId ?? Guid.NewGuid(),
            TrackingIgnore: false);
    }

    [Fact]
    public void GenerateComponentDiagramReport_CreatesHtmlFile()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        Assert.True(File.Exists(result.HtmlFilePath));
        var content = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("<html>", content);
        Assert.Contains("Component Diagram", content);
        Assert.Contains("data-plantuml-z=", content);
    }

    [Fact]
    public void GenerateComponentDiagramReport_EmptyLogs_StillGeneratesFile()
    {
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport([], new ReportConfigurationOptions { ComponentDiagramOptions = options });

        Assert.True(File.Exists(result.HtmlFilePath));
    }

    [Fact]
    public void GenerateComponentDiagramReport_CustomFileName_UsesIt()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions { FileName = "MyDiagram" };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        Assert.Contains("MyDiagram.html", result.HtmlFilePath);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PlantUmlContent_IsCorrect()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "WebApp", serviceName: "OrderService", method: "POST"),
            MakeRequest(callerName: "OrderService", serviceName: "PaymentService", method: "POST")
        };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        // Default is BrowserJs which uses plain PlantUML
        Assert.Contains("rectangle", result.PlantUml);
        Assert.Contains("WebApp", result.PlantUml);
        Assert.Contains("OrderService", result.PlantUml);
        Assert.Contains("PaymentService", result.PlantUml);
        Assert.Contains("-[#438DD5]->", result.PlantUml);
    }

    [Fact]
    public void GenerateComponentDiagramReport_HtmlContainsImageTag()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options, PlantUmlRendering = PlantUmlRendering.Server });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("<img src=", html);
        Assert.Contains("plantuml.com/plantuml", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_HtmlEncodesPlantUmlSource()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        var html = File.ReadAllText(result.HtmlFilePath);
        // BrowserJs default: diagram source is compressed in data-plantuml-z
        Assert.Contains("data-plantuml-z=", html);
        // BrowserJs uses plain PlantUML with skinparams (verify via result.PlantUml)
        Assert.Contains("skinparam", result.PlantUml);
        Assert.DoesNotContain("C4_Context", result.PlantUml);
        Assert.DoesNotContain("raw.githubusercontent.com", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_ServerUrl_ContainsEncodedPlantUml()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = options, PlantUmlServerBaseUrl = "https://plantuml.com/plantuml", PlantUmlImageFormat = PlantUmlImageFormat.Svg, PlantUmlRendering = PlantUmlRendering.Server });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("plantuml.com/plantuml/svg/", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PngFormat_UsesPngUrl()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = options, PlantUmlImageFormat = PlantUmlImageFormat.Png, PlantUmlRendering = PlantUmlRendering.Server });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("plantuml.com/plantuml/png/", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_LocalRenderer_UsesRenderedImage()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();
        var fakeImageBytes = "fake-image-data"u8.ToArray();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = options,
                PlantUmlImageFormat = PlantUmlImageFormat.Base64Png,
                PlantUmlRendering = PlantUmlRendering.Local,
                LocalDiagramRenderer = (_, _) => fakeImageBytes
            });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("data:image/png;base64,", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PlantUml_UsesPlainPlantUmlByDefault()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        // Default BrowserJs mode uses plain PlantUML, no C4
        Assert.DoesNotContain("!include", result.PlantUml);
        Assert.Contains("skinparam", result.PlantUml);
        Assert.Contains("rectangle", result.PlantUml);
    }

    // ── Browser SVG rendering ──

    [Fact]
    public void GenerateComponentDiagramReport_BrowserJs_Uses_PlantumlBrowserDiv()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = options,
                PlantUmlRendering = PlantUmlRendering.BrowserJs
            });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("plantuml-browser", html);
        Assert.Contains("data-plantuml", html);
        Assert.Contains("plantuml.js", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_Server_StillUsesImgTag()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = options,
                PlantUmlRendering = PlantUmlRendering.Server
            });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("<img src=", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_BrowserJs_DoesNotContainC4Include()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = new ComponentDiagramOptions(),
                PlantUmlRendering = PlantUmlRendering.BrowserJs
            });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("!include", html);
        Assert.DoesNotContain("C4_Context", html);
        Assert.DoesNotContain("Person(", html);
        Assert.DoesNotContain("System(", html);
        // Should contain the browser-compatible skinparam styling (verify via PlantUml output)
        Assert.Contains("skinparam", result.PlantUml);
        Assert.Contains("rectangle", result.PlantUml);
    }

    [Fact]
    public void GenerateComponentDiagramReport_Server_ContainsC4Include()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = new ComponentDiagramOptions(),
                PlantUmlRendering = PlantUmlRendering.Server
            });

        Assert.Contains("!include <C4/C4_Context>", result.PlantUml);
        Assert.Contains("Person(", result.PlantUml);
    }

    // ── Left-to-right direction ──

    [Fact]
    public void GenerateComponentDiagramReport_PlantUml_ContainsLeftToRight()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        Assert.Contains("left to right direction", result.PlantUml);
    }

    // ── PlantUML source section removed ──

    [Fact]
    public void GenerateComponentDiagramReport_Html_DoesNotContainPlantUmlSourceSection()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("<summary><strong>PlantUML Source</strong></summary>", html);
        Assert.DoesNotContain("diagram-container", html);
    }

    // ── Simplified output does not include removed features ──

    [Fact]
    public void GenerateComponentDiagramReport_DoesNotIncludeSystemFlowOrRelationshipFlows()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("iflow-rel-list", html);
        Assert.DoesNotContain("System Flow", html);
        Assert.DoesNotContain("window.__iflowSegments = {", html);
        Assert.DoesNotContain("focus-dimmed", html);
        Assert.DoesNotContain("focusNode", html);
        Assert.DoesNotContain("performance-summary", html);
        Assert.DoesNotContain("latency-chart", html);
    }

    [Fact]
    public void ComponentDiagramOptions_ShowRelationshipFlows_DefaultsToTrue()
    {
        var options = new ComponentDiagramOptions();
        Assert.True(options.ShowRelationshipFlows);
    }

    [Fact]
    public void ComponentDiagramOptions_ShowSystemFlameChart_DefaultsToTrue()
    {
        var options = new ComponentDiagramOptions();
        Assert.True(options.ShowSystemFlameChart);
    }

    // ═══════════════════════════════════════════════════════════
    // Embed Component Diagram in TestRunReport
    // ═══════════════════════════════════════════════════════════

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

    private static DefaultDiagramsFetcher.DiagramAsCode[] MakeDiagrams(string testId = "test-1") =>
    [
        new DefaultDiagramsFetcher.DiagramAsCode(testId, "",
            "@startuml\nAlice -> Bob: Hello\n@enduml\n")
    ];

    [Fact]
    public void EmbeddedComponentDiagram_Appears_In_TestRunReport_When_Enabled()
    {
        var plantUml = "@startuml\nleft to right direction\nrectangle A\n@enduml";

        var html = ReportGenerator.GenerateHtmlReport(
            MakeDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "EmbedComponentTest.html", "Test", true,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            componentDiagramPlantUml: plantUml);

        var content = File.ReadAllText(html);
        Assert.Contains("component-diagram-section", content);
        Assert.Contains("Component Diagram", content);
        Assert.Contains("data-plantuml-z=", content);
        Assert.Contains("plantuml-browser", content);
    }

    [Fact]
    public void EmbeddedComponentDiagram_Is_Hidden_By_Default()
    {
        var plantUml = "@startuml\nleft to right direction\nrectangle A\n@enduml";

        var html = ReportGenerator.GenerateHtmlReport(
            MakeDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "EmbedComponentHidden.html", "Test", true,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            componentDiagramPlantUml: plantUml);

        var content = File.ReadAllText(html);
        Assert.Contains("""id="component-diagram""", content);
        Assert.Contains("""style="display:none""", content);
        Assert.DoesNotContain("<details", content.Substring(content.IndexOf("component-diagram-section"),
            content.IndexOf("report-content") - content.IndexOf("component-diagram-section")));
    }

    [Fact]
    public void EmbeddedComponentDiagram_Has_Toggle_Button_In_Toolbar()
    {
        var plantUml = "@startuml\nleft to right direction\nrectangle A\n@enduml";

        var html = ReportGenerator.GenerateHtmlReport(
            MakeDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "EmbedComponentToggleBtn.html", "Test", true,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            componentDiagramPlantUml: plantUml);

        var content = File.ReadAllText(html);
        Assert.Contains("toggle_component_diagram(this)", content);
        Assert.Contains(">Component Diagram</button>", content);
    }

    [Fact]
    public void EmbeddedComponentDiagram_No_Toggle_Button_When_Null()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakeDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "NoComponentToggle.html", "Test", true,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            componentDiagramPlantUml: null);

        var content = File.ReadAllText(html);
        Assert.DoesNotContain("toggle_component_diagram", content);
    }

    [Fact]
    public void EmbeddedComponentDiagram_Toggle_Function_Exists_In_Script()
    {
        var plantUml = "@startuml\nleft to right direction\nrectangle A\n@enduml";

        var html = ReportGenerator.GenerateHtmlReport(
            MakeDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "EmbedComponentToggleJs.html", "Test", true,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            componentDiagramPlantUml: plantUml);

        var content = File.ReadAllText(html);
        Assert.Contains("function toggle_component_diagram(btn)", content);
        Assert.Contains("_renderDiagramsInContainer", content);
    }

    [Fact]
    public void EmbeddedComponentDiagram_Not_Present_When_Null()
    {
        var html = ReportGenerator.GenerateHtmlReport(
            MakeDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "NoComponentTest.html", "Test", true,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            componentDiagramPlantUml: null);

        var content = File.ReadAllText(html);
        Assert.DoesNotContain("component-diagram-section", content);
    }

    [Fact]
    public void EmbeddedComponentDiagram_Section_Appears_Before_ReportContent()
    {
        var plantUml = "@startuml\nA -> B\n@enduml";

        var html = ReportGenerator.GenerateHtmlReport(
            MakeDiagrams(), MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, "ComponentOrderTest.html", "Test", true,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            componentDiagramPlantUml: plantUml);

        var content = File.ReadAllText(html);
        var componentIdx = content.IndexOf("component-diagram-section");
        var reportContentIdx = content.IndexOf("id=\"report-content\"");
        Assert.True(componentIdx > 0 && componentIdx < reportContentIdx,
            "Component diagram section must appear before report-content div");
    }
}
