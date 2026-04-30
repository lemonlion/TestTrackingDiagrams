using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class ZoomNoteInteractionTests : IClassFixture<ChromeFixture>, IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(ZoomNoteInteractionTests).Assembly.Location)!,
        "SeleniumOutput");

    public ZoomNoteInteractionTests(ChromeFixture chrome)
    {
        _driver = chrome.Driver;
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-zni-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    private string GenerateReport(string fileName) =>
        ReportTestHelper.GenerateReportWithWideNoteDiagram(_tempDir, OutputDir, fileName);

    private IWebElement WaitFor(By by, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private void WaitForDiagramSvg(int timeoutSeconds = 20)
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(document.body);");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        wait.Until(d =>
        {
            try
            {
                var svg = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                return svg.Displayed ? svg : null;
            }
            catch (NoSuchElementException) { return null; }
        });
    }

    private IWebElement GetDiagramContainer() =>
        _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml']"));

    private void ExpandFirstScenarioWithDiagram()
    {
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]")).Click();
    }

    private void ZoomIn()
    {
        var container = GetDiagramContainer();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => container.FindElements(By.CssSelector(".diagram-zoom-toggle")).Count > 0);
        var zoomBtn = container.FindElement(By.CssSelector(".diagram-zoom-toggle"));
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", zoomBtn);
        wait.Until(d => container.GetAttribute("class")!.Contains("diagram-natural-size"));
    }

    private void WaitForReRender(IWebElement container)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        // Wait for re-render: SVG exists, zoom button exists, and no rendering in progress
        wait.Until(d =>
        {
            try
            {
                var svg = container.FindElement(By.TagName("svg"));
                if (!svg.Displayed) return false;
                var rendering = (bool)((IJavaScriptExecutor)d).ExecuteScript(
                    "return !!arguments[0]._noteRendering || !!window._plantumlRendering;", container);
                return !rendering;
            }
            catch (StaleElementReferenceException) { return false; }
            catch (NoSuchElementException) { return false; }
        });
    }

    private void ClickRadioButton(string state)
    {
        var btn = _driver.FindElement(By.CssSelector(
            $".diagram-toggle .details-radio-btn[data-state='{state}']"));
        btn.Click();
    }

    private bool IsZoomedIn(IWebElement container)
    {
        return container.GetAttribute("class")!.Contains("diagram-natural-size");
    }

    private string GetSvgMaxWidth(IWebElement container)
    {
        return (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "var svg = arguments[0].querySelector('svg'); return svg ? svg.style.maxWidth : '';",
            container);
    }

    private string GetContainerOverflow(IWebElement container)
    {
        return (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return arguments[0].style.overflow;", container);
    }

    // ── Zoom state preserved after note collapse ──

    [Fact]
    public void Zoom_state_preserved_after_note_collapse_via_radio()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ZoomPreservedAfterCollapse.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        ZoomIn();
        var container = GetDiagramContainer();
        Assert.True(IsZoomedIn(container));
        Assert.Equal("none", GetSvgMaxWidth(container));

        // Collapse notes via radio
        ClickRadioButton("collapsed");
        WaitForReRender(container);

        // Zoom state should be preserved
        Assert.True(IsZoomedIn(container), "Container should still have diagram-natural-size class");
        Assert.Equal("none", GetSvgMaxWidth(container));
        Assert.Equal("auto", GetContainerOverflow(container));
    }

    [Fact]
    public void Zoom_state_preserved_after_truncation_change()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ZoomPreservedAfterTruncation.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        ZoomIn();
        var container = GetDiagramContainer();
        Assert.True(IsZoomedIn(container));

        // Change to expanded (triggers re-render)
        ClickRadioButton("expanded");
        WaitForReRender(container);

        // Zoom state should be preserved
        Assert.True(IsZoomedIn(container));
        Assert.Equal("none", GetSvgMaxWidth(container));
        Assert.Equal("auto", GetContainerOverflow(container));
    }

    [Fact]
    public void Zoom_state_preserved_after_headers_toggle()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ZoomPreservedAfterHeaders.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        ZoomIn();
        var container = GetDiagramContainer();
        Assert.True(IsZoomedIn(container));

        // Toggle headers off (triggers re-render) — only if headers toggle exists
        var headersHiddenButtons = _driver.FindElements(By.CssSelector(
            ".diagram-toggle .headers-radio-btn[data-hstate='hidden']"));
        if (headersHiddenButtons.Count == 0)
        {
            // No headers toggle in this report — skip
            return;
        }
        headersHiddenButtons[0].Click();
        WaitForReRender(container);

        // Zoom state should be preserved
        Assert.True(IsZoomedIn(container));
        Assert.Equal("none", GetSvgMaxWidth(container));
    }

    // ── Zoom toggle correct after re-render ──

    [Fact]
    public void Zoom_toggle_out_works_correctly_after_note_collapse()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ZoomToggleAfterCollapse.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        ZoomIn();
        var container = GetDiagramContainer();

        // Collapse notes (re-render)
        ClickRadioButton("collapsed");
        WaitForReRender(container);

        // Now toggle zoom OFF — should actually zoom out
        var zoomBtn = container.FindElement(By.CssSelector(".diagram-zoom-toggle"));
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", zoomBtn);

        Assert.False(IsZoomedIn(container), "Should have zoomed out");
        Assert.Equal("", GetSvgMaxWidth(container));
        Assert.Equal("", GetContainerOverflow(container));
    }

    [Fact]
    public void Zoom_toggle_in_again_after_collapse_and_unzoom()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ZoomToggleInAgain.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        ZoomIn();
        var container = GetDiagramContainer();

        // Collapse notes
        ClickRadioButton("collapsed");
        WaitForReRender(container);

        // Zoom out
        var zoomBtn = container.FindElement(By.CssSelector(".diagram-zoom-toggle"));
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", zoomBtn);
        Assert.False(IsZoomedIn(container));

        // Zoom in again — should work normally
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", zoomBtn);
        Assert.True(IsZoomedIn(container));
        Assert.Equal("none", GetSvgMaxWidth(container));
    }

    // ── Note button interactions while zoomed ──

    [Fact]
    public void Note_collapse_button_works_while_zoomed()
    {
        _driver.Navigate().GoToUrl(GenerateReport("NoteCollapseWhileZoomed.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        ZoomIn();
        var container = GetDiagramContainer();

        // Double-click first note to cycle state
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            var rects = arguments[0].querySelectorAll('.note-hover-rect');
            if (rects.length > 0) {
                var evt = new MouseEvent('dblclick', {bubbles: true});
                rects[0].dispatchEvent(evt);
            }
        ", container);

        // Wait for re-render
        WaitForReRender(container);

        // Should still be zoomed
        Assert.True(IsZoomedIn(container));
        Assert.Equal("none", GetSvgMaxWidth(container));
    }

    [Fact]
    public void Multiple_note_state_changes_while_zoomed_preserves_zoom()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MultiNoteChangesZoomed.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        ZoomIn();
        var container = GetDiagramContainer();

        // Collapse all
        ClickRadioButton("collapsed");
        WaitForReRender(container);
        Assert.True(IsZoomedIn(container));

        // Expand all
        ClickRadioButton("expanded");
        WaitForReRender(container);
        Assert.True(IsZoomedIn(container));

        // Truncate
        ClickRadioButton("truncated");
        WaitForReRender(container);
        Assert.True(IsZoomedIn(container));
        Assert.Equal("none", GetSvgMaxWidth(container));
    }

    // ── Zoom after unzoomed state + re-render ──

    [Fact]
    public void Unzoomed_state_not_affected_by_note_collapse()
    {
        _driver.Navigate().GoToUrl(GenerateReport("UnzoomedNotAffected.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var container = GetDiagramContainer();
        Assert.False(IsZoomedIn(container));

        // Collapse notes
        ClickRadioButton("collapsed");
        WaitForReRender(container);

        // Should remain unzoomed
        Assert.False(IsZoomedIn(container));
        Assert.Equal("", GetSvgMaxWidth(container));
    }

    [Fact]
    public void Zoom_in_works_after_unzoomed_collapse()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ZoomInAfterCollapse.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var container = GetDiagramContainer();

        // Collapse notes while unzoomed
        ClickRadioButton("collapsed");
        WaitForReRender(container);

        // Now zoom in — should work
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => container.FindElements(By.CssSelector(".diagram-zoom-toggle")).Count > 0);
        var zoomBtn = container.FindElement(By.CssSelector(".diagram-zoom-toggle"));
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", zoomBtn);

        Assert.True(IsZoomedIn(container));
        Assert.Equal("none", GetSvgMaxWidth(container));
    }

    // ── Rapid toggling ──

    [Fact]
    public void Rapid_zoom_toggle_produces_consistent_state()
    {
        _driver.Navigate().GoToUrl(GenerateReport("RapidZoomToggle.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var container = GetDiagramContainer();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => container.FindElements(By.CssSelector(".diagram-zoom-toggle")).Count > 0);
        var zoomBtn = container.FindElement(By.CssSelector(".diagram-zoom-toggle"));

        // Toggle 4 times rapidly (should end up unzoomed: off → on → off → on → off)
        for (int i = 0; i < 4; i++)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", zoomBtn);
        }

        // Even number of toggles → should be back to unzoomed
        Assert.False(IsZoomedIn(container));
        Assert.Equal("", GetSvgMaxWidth(container));
    }

    [Fact]
    public void Zoom_icon_correct_after_collapse_re_render()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ZoomIconAfterCollapse.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        ZoomIn();
        var container = GetDiagramContainer();

        // Collapse notes
        ClickRadioButton("collapsed");
        WaitForReRender(container);

        // The zoom button icon should show the "zoom out" icon (⤡) since we're still zoomed
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => container.FindElements(By.CssSelector(".diagram-zoom-toggle")).Count > 0);
        var zoomBtn = container.FindElement(By.CssSelector(".diagram-zoom-toggle"));
        var iconText = (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return arguments[0].textContent;", zoomBtn);
        Assert.Equal("\u2921", iconText); // ⤡ = zoomed-in icon
    }

    [Fact]
    public void Zoom_icon_correct_when_unzoomed_after_collapse()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ZoomIconUnzoomedCollapse.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        var container = GetDiagramContainer();

        // Collapse notes while unzoomed
        ClickRadioButton("collapsed");
        WaitForReRender(container);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => container.FindElements(By.CssSelector(".diagram-zoom-toggle")).Count > 0);
        var zoomBtn = container.FindElement(By.CssSelector(".diagram-zoom-toggle"));
        var iconText = (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return arguments[0].textContent;", zoomBtn);
        Assert.Equal("\u2922", iconText); // ⤢ = unzoomed icon
    }

    // ── Report-level controls ──

    [Fact]
    public void Report_level_truncation_change_preserves_zoom()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ReportTruncationPreservesZoom.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        ZoomIn();
        var container = GetDiagramContainer();

        // Use report-level truncation change (the dropdown at toolbar level)
        var reportExpandedBtn = _driver.FindElements(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='expanded']"));
        if (reportExpandedBtn.Count > 0)
        {
            reportExpandedBtn[0].Click();
            WaitForReRender(container);
            Assert.True(IsZoomedIn(container));
            Assert.Equal("none", GetSvgMaxWidth(container));
        }
    }

    [Fact]
    public void Report_level_headers_toggle_preserves_zoom()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ReportHeadersPreservesZoom.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();

        ZoomIn();
        var container = GetDiagramContainer();

        // Use report-level headers toggle
        var reportHeadersHidden = _driver.FindElements(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='hidden']"));
        if (reportHeadersHidden.Count > 0)
        {
            reportHeadersHidden[0].Click();
            WaitForReRender(container);
            Assert.True(IsZoomedIn(container));
            Assert.Equal("none", GetSvgMaxWidth(container));
        }
    }
}
