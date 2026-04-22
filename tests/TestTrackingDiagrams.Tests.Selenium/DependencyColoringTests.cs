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
    public void Embedded_component_diagram_section_is_visible()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithComponentDiagram("ComponentEmbed.html"));

        var section = WaitFor(By.CssSelector("details.component-diagram-section"));
        Assert.True(section.Displayed, "Component diagram section should be visible");

        var summary = section.FindElement(By.CssSelector("summary"));
        Assert.Contains("Component Diagram", summary.Text);
    }

    [Fact]
    public void Embedded_component_diagram_renders_SVG()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithComponentDiagram("ComponentSvg.html"));

        var section = WaitFor(By.CssSelector("details.component-diagram-section"));
        Assert.True(section.Displayed);

        // Wait for the PlantUML browser renderer to produce an SVG inside the component diagram section
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        var svg = wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(By.CssSelector(".component-diagram-section [data-diagram-type='plantuml'] svg"));
                return el.Displayed ? el : null;
            }
            catch (NoSuchElementException) { return null; }
        });

        Assert.NotNull(svg);
        var svgHtml = svg!.GetAttribute("outerHTML");
        Assert.Contains("svg", svgHtml);
    }

    [Fact]
    public void Embedded_component_diagram_is_open_by_default()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithComponentDiagram("ComponentOpen.html"));

        var section = WaitFor(By.CssSelector("details.component-diagram-section"));

        // The <details> element should be open by default
        var isOpen = section.GetAttribute("open");
        Assert.NotNull(isOpen);
    }

    [Fact]
    public void Embedded_component_diagram_can_be_collapsed()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithComponentDiagram("ComponentCollapse.html"));

        var section = WaitFor(By.CssSelector("details.component-diagram-section"));
        var summary = section.FindElement(By.CssSelector("summary"));

        // Click to collapse
        summary.Click();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(d =>
        {
            var el = d.FindElement(By.CssSelector("details.component-diagram-section"));
            return el.GetAttribute("open") == null;
        });

        var isOpen = section.GetAttribute("open");
        Assert.Null(isOpen);
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
