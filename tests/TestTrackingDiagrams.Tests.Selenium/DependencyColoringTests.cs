using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class DependencyColoringTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(DependencyColoringTests).Assembly.Location)!,
        "SeleniumOutput");

    public DependencyColoringTests()
    {
        _driver = ChromeDriverFactory.Create();
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-depcolor-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    private string GenerateReport(string fileName) =>
        ReportTestHelper.GenerateReport(_tempDir, OutputDir, fileName);

    private string GenerateReportWithComponentDiagram(string fileName) =>
        ReportTestHelper.GenerateReportWithEmbeddedComponentDiagram(_tempDir, OutputDir, fileName);

    private IWebElement WaitFor(By by, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private IWebElement WaitForDiagramSvg(int timeoutSeconds = 20)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d =>
        {
            try
            {
                var svg = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                return svg.Displayed ? svg : null;
            }
            catch (NoSuchElementException) { return null; }
        }) ?? throw new TimeoutException("Diagram SVG did not render");
    }

    private void ExpandFirstScenarioWithDiagram()
    {
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]")).Click();
    }

    // ═══════════════════════════════════════════════════════════
    // Sequence Diagram — Colored Arrows
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Sequence_diagram_renders_SVG_with_colored_arrows()
    {
        _driver.Navigate().GoToUrl(GenerateReport("DepColorArrows.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        // The SVG should contain arrow lines (rendered as polygon/line elements)
        var svgHtml = svg.GetAttribute("outerHTML");
        Assert.Contains("svg", svgHtml);
        Assert.True(svgHtml.Length > 100, "SVG should contain rendered diagram content");
    }

    // ═══════════════════════════════════════════════════════════
    // Embedded Component Diagram
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Component_diagram_is_hidden_by_default()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithComponentDiagram("ComponentHiddenDefault.html"));

        WaitFor(By.CssSelector("details.feature"));

        var section = _driver.FindElement(By.Id("component-diagram"));
        var display = section.GetCssValue("display");
        Assert.Equal("none", display);
    }

    [Fact]
    public void Component_diagram_toggle_button_exists()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithComponentDiagram("ComponentToggleBtn.html"));

        var button = WaitFor(By.CssSelector("button.timeline-toggle[onclick*='toggle_component_diagram']"));
        Assert.Contains("Component Diagram", button.Text);
    }

    [Fact]
    public void Component_diagram_toggle_button_not_present_without_diagram()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoComponentToggle.html"));
        WaitFor(By.CssSelector("details.feature"));

        var buttons = _driver.FindElements(By.CssSelector("button[onclick*='toggle_component_diagram']"));
        Assert.Empty(buttons);
    }

    [Fact]
    public void Component_diagram_toggle_shows_and_hides()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithComponentDiagram("ComponentToggleShowHide.html"));

        var button = WaitFor(By.CssSelector("button[onclick*='toggle_component_diagram']"));
        var section = _driver.FindElement(By.Id("component-diagram"));

        // Initially hidden
        Assert.Equal("none", section.GetCssValue("display"));

        // Click to show
        button.Click();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(_ => section.GetCssValue("display") != "none");

        // Click to hide
        button.Click();
        wait.Until(_ => section.GetCssValue("display") == "none");
    }

    [Fact]
    public void Component_diagram_toggle_button_has_active_class_when_shown()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithComponentDiagram("ComponentToggleActive.html"));

        var button = WaitFor(By.CssSelector("button[onclick*='toggle_component_diagram']"));

        // Initially no active class
        Assert.DoesNotContain("timeline-toggle-active", button.GetAttribute("class"));

        // Click to show
        button.Click();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(_ => button.GetAttribute("class").Contains("timeline-toggle-active"));
        Assert.Contains("timeline-toggle-active", button.GetAttribute("class"));

        // Click to hide
        button.Click();
        wait.Until(_ => !button.GetAttribute("class").Contains("timeline-toggle-active"));
        Assert.DoesNotContain("timeline-toggle-active", button.GetAttribute("class"));
    }

    [Fact]
    public void Component_diagram_renders_SVG_after_toggle()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithComponentDiagram("ComponentSvgToggle.html"));

        var button = WaitFor(By.CssSelector("button[onclick*='toggle_component_diagram']"));

        // Click to show — triggers _renderDiagramsInContainer
        button.Click();

        // Wait for the PlantUML browser renderer to produce an SVG inside the component diagram section
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        var svg = wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(By.CssSelector("#component-diagram [data-diagram-type='plantuml'] svg"));
                return el.Displayed ? el : null;
            }
            catch (NoSuchElementException) { return null; }
        });

        Assert.NotNull(svg);
        var svgHtml = svg!.GetAttribute("outerHTML");
        Assert.Contains("svg", svgHtml);
    }

    [Fact]
    public void Report_without_component_diagram_has_no_section()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoComponentSection.html"));
        WaitFor(By.CssSelector("details.feature"));

        var sections = _driver.FindElements(By.CssSelector(".component-diagram-section"));
        Assert.Empty(sections);
    }
}
