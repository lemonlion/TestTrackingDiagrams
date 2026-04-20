using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

/// <summary>
/// Captures specific screenshots for the "What's New in 2.0" wiki page.
/// Run: dotnet test TestTrackingDiagrams.Tests.Selenium --filter "WikiScreenshotTests"
/// Output goes to bin/SeleniumOutput/wiki-screenshots/
/// </summary>
public class WikiScreenshotTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _outputDir;

    private static readonly string BaseOutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(WikiScreenshotTests).Assembly.Location)!,
        "SeleniumOutput");

    private static readonly string ReportsDir = Path.Combine(
        Path.GetDirectoryName(typeof(WikiScreenshotTests).Assembly.Location)!,
        "..", "..", "..", "..", "TestTrackingDiagrams.Tests", "bin", "Debug", "net10.0", "Reports");

    private static readonly string ShowcaseReportPath = Path.Combine(
        BaseOutputDir, "ShowcaseReport.html");

    public WikiScreenshotTests()
    {
        _driver = ChromeDriverFactory.Create(1280, 900);
        _outputDir = Path.Combine(BaseOutputDir, "wiki-screenshots");
        Directory.CreateDirectory(_outputDir);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }

    private void SaveScreenshot(string name)
    {
        var screenshot = _driver.GetScreenshot();
        screenshot.SaveAsFile(Path.Combine(_outputDir, name));
    }

    private IWebElement WaitFor(By by, int timeoutSeconds = 10)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private void Pause(int ms) => Thread.Sleep(ms);

    private void OpenFile(string path)
    {
        var uri = new Uri(Path.GetFullPath(path)).AbsoluteUri;
        _driver.Navigate().GoToUrl(uri);
        Pause(500);
    }

    private void ExpandAll()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.querySelectorAll('details').forEach(d => d.open = true);");
        Pause(300);
    }

    private void ScrollTo(IWebElement element)
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'start',behavior:'instant'});", element);
        Pause(300);
    }

    private void ScrollToTop()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0,0);");
        Pause(300);
    }

    // ── Feature 7: Component Diagram ──
    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Capture_component_diagram_report()
    {
        var file = Path.Combine(ReportsDir, "ComponentSpecificationsWithExamples.html");
        if (!File.Exists(file))
        {
            // Fallback — use archived report from Example.Api
            var archived = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(typeof(WikiScreenshotTests).Assembly.Location)!,
                "..", "..", "..", "..", "Example.Api", "TestResults", "ArchivedReports", "BDDfy.xUnit3",
                "ComponentDiagram.Project_report_contains_expected_scenarios.html"));
            if (File.Exists(archived)) file = archived;
        }
        Assert.True(File.Exists(file), $"Report file not found: {file}");
        OpenFile(file);
        WaitFor(By.CssSelector("details.feature, details, body"));
        ExpandAll();
        Pause(1000);

        // Wait for PlantUML diagrams to render
        try
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
            wait.Until(d =>
            {
                try { return d.FindElement(By.CssSelector("svg, [data-diagram-type] img")).Displayed; }
                catch { return false; }
            });
        }
        catch { /* Proceed even if diagrams don't render */ }

        // Scroll to show the most interesting content
        ScrollToTop();
        Pause(500);
        SaveScreenshot("whats-new-component-diagram.png");
    }

    // ── Feature 8: Internal Flow (OpenTelemetry) ──
    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Capture_internal_flow_visualization()
    {
        // Use the whole-test-flow test page that has Activity + Flame chart
        var file = Path.Combine(BaseOutputDir, "Whole_test_flow_Both_shows_toggle_buttons.html");
        if (!File.Exists(file))
        {
            // Fallback: try toggle options report
            file = Path.Combine(ReportsDir, "ToggleBoth.html");
        }
        if (!File.Exists(file))
        {
            file = Path.Combine(ReportsDir, "ToggleFlowOnly.html");
        }
        Assert.True(File.Exists(file), "Internal flow report not found");
        OpenFile(file);
        Pause(500);

        // Try to expand the flow section
        try
        {
            ExpandAll();
            var flowDetails = _driver.FindElements(By.CssSelector("details.wtf-block, details"));
            foreach (var d in flowDetails)
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].open = true;", d);
            }
        }
        catch { /* best effort */ }
        Pause(500);
        SaveScreenshot("whats-new-internal-flow.png");
    }

    // ── Feature 9: CI/CD Summary ──
    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Capture_ci_summary()
    {
        var file = Path.Combine(ReportsDir, "CiSummaryInteractive.html");
        Assert.True(File.Exists(file), "CI Summary report not found");
        OpenFile(file);
        WaitFor(By.TagName("body"));
        Pause(500);
        SaveScreenshot("whats-new-ci-summary.png");
    }

    // ── Feature 10: Machine-Readable Formats ──
    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Capture_test_run_report()
    {
        // TestRunReport.html shows metadata and embedded diagrams
        var file = Path.Combine(ReportsDir, "TestRunReport.html");
        if (!File.Exists(file))
        {
            file = Path.Combine(ReportsDir, "Specifications.html");
        }
        Assert.True(File.Exists(file), "TestRunReport not found");
        OpenFile(file);
        WaitFor(By.TagName("body"));
        ExpandAll();
        Pause(500);
        SaveScreenshot("whats-new-machine-readable.png");
    }

    // ── Feature 12: DiagramFocus ──
    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Capture_diagram_focus()
    {
        // Look for a Focus report with bold emphasis
        var candidates = new[]
        {
            Path.Combine(ReportsDir, "TestRunReport.FocusBoldEmphasis_HighlightsRequestFields.html"),
            Path.Combine(ReportsDir, "TestRunReport.FocusBoldAndColoredEmphasis_CombinesMarkup.html"),
            Path.Combine(ReportsDir, "TestRunReport.FocusColoredEmphasis_HighlightsRequestFields.html"),
        };
        var file = candidates.FirstOrDefault(File.Exists);

        if (file == null)
        {
            // Try archived reports
            var archiveBase = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(typeof(WikiScreenshotTests).Assembly.Location)!,
                "..", "..", "..", "..", "Example.Api", "TestResults", "ArchivedReports", "BDDfy.xUnit3"));
            candidates = new[]
            {
                Path.Combine(archiveBase, "TestRunReport.FocusBoldEmphasis_HighlightsRequestFields.html"),
                Path.Combine(archiveBase, "TestRunReport.FocusBoldAndColoredEmphasis_CombinesMarkup.html"),
            };
            file = candidates.FirstOrDefault(File.Exists);
        }

        Assert.NotNull(file);
        OpenFile(file!);
        WaitFor(By.TagName("body"));
        ExpandAll();
        Pause(500);

        // Wait for diagram rendering
        try
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
            wait.Until(d =>
            {
                try { return d.FindElement(By.CssSelector("svg")).Displayed; }
                catch { return false; }
            });
        }
        catch { /* Proceed anyway */ }

        // Scroll to the diagram area (look for diagram container)
        try
        {
            var diagramArea = _driver.FindElement(By.CssSelector("[data-diagram-type], .scenario"));
            ScrollTo(diagramArea);
        }
        catch { /* stay at current position */ }
        Pause(300);
        SaveScreenshot("whats-new-diagram-focus.png");
    }

    // ── Feature 19: Scenario Timeline ──
    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Capture_scenario_timeline()
    {
        // Use a timeline report that has the timeline section
        var file = Path.Combine(ReportsDir, "TimelineBars.html");
        if (!File.Exists(file)) file = Path.Combine(ReportsDir, "TimelineToggle.html");
        Assert.True(File.Exists(file), "Timeline report not found");
        OpenFile(file);
        WaitFor(By.TagName("body"));
        Pause(500);

        // Click the "Scenario Timeline" button if it exists
        try
        {
            var timelineBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Scenario Timeline')] | //button[contains(text(),'Timeline')]"));
            timelineBtn.Click();
            Pause(500);
        }
        catch
        {
            // Timeline might be in a details element
            ExpandAll();
            Pause(500);
        }

        SaveScreenshot("whats-new-scenario-timeline.png");
    }

    // ── Feature 20: Export ──
    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Capture_export_buttons()
    {
        // Open the showcase report (if available) to show export buttons
        if (File.Exists(ShowcaseReportPath))
        {
            OpenFile(ShowcaseReportPath);
        }
        else
        {
            // Fall back to any report with export functionality
            var file = Path.Combine(ReportsDir, "ExportBtn.html");
            if (!File.Exists(file)) file = Path.Combine(ReportsDir, "ExportHtml.html");
            Assert.True(File.Exists(file), "Export report not found");
            OpenFile(file);
        }
        WaitFor(By.TagName("body"));
        Pause(500);

        // Focus on the filtering area with export buttons
        try
        {
            var exportBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Export')]"));
            ScrollTo(exportBtn);
        }
        catch { /* stay at top */ }
        Pause(300);

        ScrollToTop();
        Pause(300);
        SaveScreenshot("whats-new-export.png");
    }

    // ── Feature 15: Violet Theme (use showcase report overview) ──
    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Capture_violet_theme_overview()
    {
        if (File.Exists(ShowcaseReportPath))
        {
            OpenFile(ShowcaseReportPath);
        }
        else
        {
            var file = Path.Combine(ReportsDir, "PieChart.html");
            if (!File.Exists(file)) file = Path.Combine(ReportsDir, "Specifications.html");
            Assert.True(File.Exists(file), "Report not found");
            OpenFile(file);
        }
        WaitFor(By.TagName("body"));
        Pause(500);

        // Expand features summary
        try
        {
            var summary = _driver.FindElement(By.XPath("//details[contains(.,'Features Summary')]"));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].open = true;", summary);
            Pause(300);
        }
        catch { /* best effort */ }

        ScrollToTop();
        Pause(300);
        SaveScreenshot("whats-new-violet-theme.png");
    }

    // ── Feature 11: DispatchProxy / MediatR (diagram with tracked service calls) ──
    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Capture_dispatch_proxy_tracking()
    {
        // Show a diagram with rich service interaction tracking
        // Use DepFilter report which shows dependency tracking
        var file = Path.Combine(ReportsDir, "DepFilterButtons.html");
        if (!File.Exists(file)) file = Path.Combine(ReportsDir, "DepFilterMultiScenario.html");
        Assert.True(File.Exists(file), "Dependency filter report not found");
        OpenFile(file);
        WaitFor(By.TagName("body"));
        ExpandAll();
        Pause(500);

        // Try to scroll to the dependency filter area
        try
        {
            var depFilter = _driver.FindElement(By.CssSelector(".dep-filter, [data-dep]"));
            ScrollTo(depFilter);
        }
        catch { ScrollToTop(); }
        Pause(300);
        SaveScreenshot("whats-new-dispatch-proxy.png");
    }
}
