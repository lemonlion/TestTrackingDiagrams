using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using TestTrackingDiagrams.Reports;
using static TestTrackingDiagrams.DefaultDiagramsFetcher;

namespace TestTrackingDiagrams.Tests.Selenium;

public class HeadersDetailsInterferenceTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(HeadersDetailsInterferenceTests).Assembly.Location)!,
        "SeleniumOutput");

    // PlantUML source WITH gray-colored headers (like real diagrams produce)
    private const string PlantUmlSourceWithHeaders = """
        @startuml
        actor "Caller" as caller
        participant "OrderService" as svc

        caller -> svc : POST /api/orders
        note left
        <color:gray >Content-Type: application/json
        <color:gray >Authorization: Bearer token123
        
        {"item":"Widget","qty":2}
        Line 2 of body
        Line 3 of body
        Line 4 of body
        Line 5 of body
        end note

        svc --> caller : 201 Created
        note right
        <color:gray >Content-Type: application/json
        <color:gray >X-Request-Id: abc-123
        
        {"id":"abc-123","status":"created"}
        end note
        @enduml
        """;

    public HeadersDetailsInterferenceTests()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1920,1080");
        _driver = new ChromeDriver(options);
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-hd-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    private string GenerateReport(string fileName)
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Order Feature",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "t1", DisplayName = "Create order", IsHappyPath = true,
                        Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1),
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "the system is running", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "When", Text = "I create an order", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "Then", Text = "the order is created", Status = ExecutionResult.Passed }
                        ]
                    }
                ]
            }
        };

        var diagrams = new[] { new DiagramAsCode("t1", "", PlantUmlSourceWithHeaders) };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(_tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(OutputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    private void WaitForDiagramRender(int timeoutSeconds = 15)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        wait.Until(d => d.FindElements(By.CssSelector("[data-plantuml] svg")).Count > 0);
    }

    private void ExpandScenario()
    {
        // Expand features, then scenarios
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]")).Click();
    }

    private string GetDataPlantuml() =>
        _driver.FindElement(By.CssSelector("[data-plantuml]")).GetAttribute("data-plantuml")!;

    // ── Test: Toggling headers preserves the details state ──

    [Fact]
    public void Toggling_headers_hidden_preserves_details_truncated_state()
    {
        _driver.Navigate().GoToUrl(GenerateReport("HD_HeadersPreservesTrunc.html"));
        ExpandScenario();
        WaitForDiagramRender();

        // Default: Details=Truncated, Headers=Shown
        // Verify truncated is active
        var truncBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='truncated']"));
        Assert.Contains("details-active", truncBtn.GetAttribute("class")!);

        // Toggle headers to Hidden
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='hidden']")).Click();

        // Details "Truncated" button should STILL be active
        truncBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='truncated']"));
        Assert.Contains("details-active", truncBtn.GetAttribute("class")!);

        // The data-plantuml should still have note content (not collapsed)
        var source = GetDataPlantuml();
        Assert.Contains("Widget", source);
    }

    [Fact]
    public void Toggling_headers_hidden_preserves_details_expanded_state()
    {
        _driver.Navigate().GoToUrl(GenerateReport("HD_HeadersPreservesExp.html"));
        ExpandScenario();
        WaitForDiagramRender();

        // Switch details to Expanded first
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='expanded']")).Click();
        Thread.Sleep(500); // Wait for re-render

        // Toggle headers to Hidden
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='hidden']")).Click();
        Thread.Sleep(500);

        // Details "Expanded" button should STILL be active
        var expandedBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='expanded']"));
        Assert.Contains("details-active", expandedBtn.GetAttribute("class")!);

        // Diagram source should have body content but NOT gray headers
        var source = GetDataPlantuml();
        Assert.Contains("Widget", source);
        Assert.DoesNotContain("color:gray", source);
    }

    [Fact]
    public void Toggling_details_expanded_preserves_headers_hidden_state()
    {
        _driver.Navigate().GoToUrl(GenerateReport("HD_DetailsPreservesHidden.html"));
        ExpandScenario();
        WaitForDiagramRender();

        // Set headers to Hidden first
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='hidden']")).Click();
        Thread.Sleep(500);

        // Then set details to Expanded
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='expanded']")).Click();
        Thread.Sleep(500);

        // Headers "Hidden" button should STILL be active
        var hiddenBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='hidden']"));
        Assert.Contains("details-active", hiddenBtn.GetAttribute("class")!);

        // The diagram source should NOT have gray headers
        var source = GetDataPlantuml();
        Assert.DoesNotContain("color:gray", source);
        // But should still have body content
        Assert.Contains("Widget", source);
    }

    [Fact]
    public void Toggling_details_collapsed_preserves_headers_hidden_state()
    {
        _driver.Navigate().GoToUrl(GenerateReport("HD_DetailsCollapsedPreservesHidden.html"));
        ExpandScenario();
        WaitForDiagramRender();

        // Set headers to Hidden
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='hidden']")).Click();
        Thread.Sleep(500);

        // Then set details to Collapsed
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='collapsed']")).Click();
        Thread.Sleep(500);

        // Headers "Hidden" should still be active
        var hiddenBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='hidden']"));
        Assert.Contains("details-active", hiddenBtn.GetAttribute("class")!);
    }

    [Fact]
    public void Toggling_headers_shown_after_hidden_preserves_details_state()
    {
        _driver.Navigate().GoToUrl(GenerateReport("HD_HeadersShownPreserves.html"));
        ExpandScenario();
        WaitForDiagramRender();

        // Set details to Expanded
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='expanded']")).Click();
        Thread.Sleep(500);

        // Toggle headers: Hidden then back to Shown
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='hidden']")).Click();
        Thread.Sleep(500);
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='shown']")).Click();
        Thread.Sleep(500);

        // Details "Expanded" should still be active
        var expandedBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='expanded']"));
        Assert.Contains("details-active", expandedBtn.GetAttribute("class")!);

        // Source should have both headers and body (fully restored)
        var source = GetDataPlantuml();
        Assert.Contains("color:gray", source);
        Assert.Contains("Widget", source);
    }

    [Fact]
    public void Scenario_level_headers_toggle_does_not_affect_details_radio_buttons()
    {
        _driver.Navigate().GoToUrl(GenerateReport("HD_ScenarioHeadersRadio.html"));
        ExpandScenario();
        WaitForDiagramRender();

        // Find scenario-level controls
        var scenario = _driver.FindElement(By.CssSelector("details.scenario"));

        // Verify scenario-level truncated is active
        var scenTruncBtn = scenario.FindElement(By.CssSelector(".details-radio-btn[data-state='truncated']"));
        Assert.Contains("details-active", scenTruncBtn.GetAttribute("class")!);

        // Toggle scenario-level headers to Hidden
        scenario.FindElement(By.CssSelector(".headers-radio-btn[data-hstate='hidden']")).Click();
        Thread.Sleep(500);

        // Scenario-level truncated should STILL be active
        scenTruncBtn = scenario.FindElement(By.CssSelector(".details-radio-btn[data-state='truncated']"));
        Assert.Contains("details-active", scenTruncBtn.GetAttribute("class")!);
    }

    [Fact]
    public void Report_level_details_change_preserves_scenario_level_headers_override()
    {
        _driver.Navigate().GoToUrl(GenerateReport("HD_ReportDetailsScenarioHeaders.html"));
        ExpandScenario();
        WaitForDiagramRender();

        // Set scenario-level headers to Hidden
        var scenario = _driver.FindElement(By.CssSelector("details.scenario"));
        scenario.FindElement(By.CssSelector(".headers-radio-btn[data-hstate='hidden']")).Click();
        Thread.Sleep(500);

        // Now change report-level details to Expanded
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='expanded']")).Click();
        Thread.Sleep(500);

        // Diagram source should be expanded but still have headers hidden
        var source = GetDataPlantuml();
        Assert.Contains("Widget", source);
        Assert.DoesNotContain("color:gray", source);
    }

    [Fact]
    public void Headers_hidden_state_initializes_noteSteps_from_default()
    {
        // This tests the core bug: buildHeadersQueue must initialize _noteSteps
        // from the default details state, not leave it empty (which causes collapse)
        _driver.Navigate().GoToUrl(GenerateReport("HD_HeadersInitNoteSteps.html"));
        ExpandScenario();
        WaitForDiagramRender();

        // Verify the diagram has content initially (truncated default)
        var source = GetDataPlantuml();
        Assert.Contains("Widget", source);

        // Toggle headers to Hidden
        _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='hidden']")).Click();
        Thread.Sleep(500);

        // The diagram should still show body content (notes not collapsed)
        source = GetDataPlantuml();
        Assert.Contains("Widget", source);
        Assert.DoesNotContain("color:gray", source);
    }
}
