using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class DiagramZoomTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(DiagramZoomTests).Assembly.Location)!,
        "SeleniumOutput");

    public DiagramZoomTests()
    {
        _driver = ChromeDriverFactory.Create();
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-zoom-" + Guid.NewGuid().ToString("N")[..8]);
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

    private string GenerateReportWithWideDiagram(string fileName) =>
        ReportTestHelper.GenerateReportWithWideDiagram(_tempDir, OutputDir, fileName);

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

    private IWebElement GetDiagramContainer() =>
        _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml']"));

    private string GetComputedStyle(IWebElement el, string prop) =>
        (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return window.getComputedStyle(arguments[0]).getPropertyValue(arguments[1]);",
            el, prop)!;

    private void ExpandFirstScenarioWithDiagram()
    {
        // Expand features
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        // Expand scenarios
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]")).Click();
    }

    // ── Zoom toggle button ──

    [Fact]
    public void Zoom_button_appears_on_diagram_container()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithWideDiagram("ZoomButtonExists.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var zoomBtns = _driver.FindElements(By.CssSelector(".diagram-zoom-toggle"));
        Assert.True(zoomBtns.Count > 0, "Zoom button should exist on diagram container");
    }

    [Fact]
    public void Zoom_button_is_hidden_until_hover()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithWideDiagram("ZoomHoverHidden.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var zoomBtn = _driver.FindElement(By.CssSelector(".diagram-zoom-toggle"));
        var opacity = GetComputedStyle(zoomBtn, "opacity");
        Assert.Equal("0", opacity);
    }

    [Fact]
    public void Zoom_button_becomes_visible_on_container_hover()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithWideDiagram("ZoomHoverVisible.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var container = GetDiagramContainer();
        new Actions(_driver).MoveToElement(container).Perform();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        var zoomBtn = wait.Until(_ =>
        {
            var btn = _driver.FindElement(By.CssSelector(".diagram-zoom-toggle"));
            var op = GetComputedStyle(btn, "opacity");
            return op != "0" ? btn : null;
        });
        Assert.NotNull(zoomBtn);
    }

    [Fact]
    public void Clicking_zoom_button_adds_natural_size_class()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithWideDiagram("ZoomClickNatural.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var container = GetDiagramContainer();
        // Hover to make button clickable
        new Actions(_driver).MoveToElement(container).Perform();
        Thread.Sleep(200);

        var zoomBtn = _driver.FindElement(By.CssSelector(".diagram-zoom-toggle"));
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", zoomBtn);

        Assert.Contains("diagram-natural-size", container.GetAttribute("class")!);
    }

    [Fact]
    public void Clicking_zoom_button_again_removes_natural_size_class()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithWideDiagram("ZoomClickToggle.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var container = GetDiagramContainer();
        new Actions(_driver).MoveToElement(container).Perform();
        Thread.Sleep(200);

        var zoomBtn = _driver.FindElement(By.CssSelector(".diagram-zoom-toggle"));
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", zoomBtn);
        Assert.Contains("diagram-natural-size", container.GetAttribute("class")!);

        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", zoomBtn);
        Assert.DoesNotContain("diagram-natural-size", container.GetAttribute("class")!);
    }

    [Fact]
    public void Zoomed_container_has_overflow_auto_and_max_height()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithWideDiagram("ZoomOverflow.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var container = GetDiagramContainer();
        new Actions(_driver).MoveToElement(container).Perform();
        Thread.Sleep(200);

        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].querySelector('.diagram-zoom-toggle').click();", container);

        Assert.Equal("auto", container.GetCssValue("overflow"));
        // max-height is set as 80vh but getComputedStyle returns resolved px value
        var maxHeight = (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return arguments[0].style.maxHeight;", container)!;
        Assert.Equal("80vh", maxHeight);
    }

    [Fact]
    public void Unzooming_clears_overflow_and_max_height()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithWideDiagram("ZoomClearOverflow.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var container = GetDiagramContainer();
        new Actions(_driver).MoveToElement(container).Perform();
        Thread.Sleep(200);

        // Zoom in then out
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "var btn = arguments[0].querySelector('.diagram-zoom-toggle'); btn.click(); btn.click();", container);

        // After unzoom, inline style.overflow should be cleared (empty string)
        var overflow = (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return arguments[0].style.overflow;", container)!;
        Assert.True(overflow is "" or "visible",
            $"Expected overflow style to be cleared but got: {overflow}");
    }

    // ── Double-click zoom ──

    [Fact]
    public void Double_click_on_svg_toggles_zoom()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithWideDiagram("ZoomDblClick.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        var container = GetDiagramContainer();
        new Actions(_driver).DoubleClick(svg).Perform();

        Assert.Contains("diagram-natural-size", container.GetAttribute("class")!);
    }

    [Fact]
    public void Double_click_again_unzooms()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithWideDiagram("ZoomDblClickToggle.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        var container = GetDiagramContainer();
        new Actions(_driver).DoubleClick(svg).Perform();
        Assert.Contains("diagram-natural-size", container.GetAttribute("class")!);

        new Actions(_driver).DoubleClick(svg).Perform();
        Assert.DoesNotContain("diagram-natural-size", container.GetAttribute("class")!);
    }

    // ── Zoom button icon changes ──

    [Fact]
    public void Zoom_button_icon_changes_when_zoomed()
    {
        _driver.Navigate().GoToUrl(GenerateReportWithWideDiagram("ZoomIcon.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var container = GetDiagramContainer();
        var zoomBtn = container.FindElement(By.CssSelector(".diagram-zoom-toggle"));
        // Use JS textContent to reliably read unicode chars
        var initialText = (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return arguments[0].textContent;", zoomBtn)!;

        new Actions(_driver).MoveToElement(container).Perform();
        Thread.Sleep(200);
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", zoomBtn);

        var zoomedText = (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return arguments[0].textContent;", zoomBtn)!;
        Assert.NotEqual(initialText, zoomedText);

        // Click again to unzoom — icon should revert
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", zoomBtn);
        var revertedText = (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return arguments[0].textContent;", zoomBtn)!;
        Assert.Equal(initialText, revertedText);
    }

    // ── Non-zoomable diagrams (already at 100% natural size) ──

    [Fact]
    public void Non_zoomable_diagram_has_no_zoom_button()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NonZoomableNoButton.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        // Small diagram at 1920px viewport fits naturally — no zoom button should exist
        var zoomBtns = _driver.FindElements(By.CssSelector(".diagram-zoom-toggle"));
        Assert.Empty(zoomBtns);
    }

    [Fact]
    public void Double_click_on_non_zoomable_diagram_does_not_zoom()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NonZoomableDblClick.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        var container = GetDiagramContainer();
        new Actions(_driver).DoubleClick(svg).Perform();

        Assert.DoesNotContain("diagram-natural-size", container.GetAttribute("class") ?? "");
    }
}
