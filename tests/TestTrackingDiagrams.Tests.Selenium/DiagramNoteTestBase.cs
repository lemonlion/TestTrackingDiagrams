using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public abstract class DiagramNoteTestBase : IClassFixture<ChromeFixture>, IDisposable
{
    protected readonly ChromeDriver _driver;
    protected readonly string _tempDir;
    protected static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(DiagramNoteTestBase).Assembly.Location)!,
        "SeleniumOutput");

    protected DiagramNoteTestBase(ChromeFixture chrome, string tempDirPrefix)
    {
        _driver = chrome.Driver;
        _tempDir = Path.Combine(Path.GetTempPath(), tempDirPrefix + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    protected string GenerateReport(string fileName) =>
        ReportTestHelper.GenerateReport(_tempDir, OutputDir, fileName);

    protected IWebElement WaitFor(By by, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    protected IWebElement WaitForDiagramSvg(int timeoutSeconds = 20)
    {
        // Force rendering — IntersectionObserver doesn't fire reliably in headless Chrome
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(document.body);");

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

    protected void ExpandFirstScenarioWithDiagram()
    {
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]")).Click();
    }

    protected string GenerateLongNoteReport(string fileName) =>
        ReportTestHelper.GenerateReportWithLongNotes(_tempDir, OutputDir, fileName);

    protected void ExpandAndRenderLongNoteDiagram(string fileName)
    {
        _driver.Navigate().GoToUrl(GenerateLongNoteReport(fileName));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        WaitForDiagramSvg();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0);
    }

    /// <summary>
    /// Hover over a note hover rect by index, retrying if the element becomes stale
    /// (can happen when the SVG is still being replaced after a state change).
    /// </summary>
    protected void HoverNoteRect(int index, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        wait.Until(d =>
        {
            try
            {
                var rects = d.FindElements(By.CssSelector(".note-hover-rect"));
                if (rects.Count <= index) return false;
                new Actions(_driver).MoveToElement(rects[index]).Perform();
                return true;
            }
            catch (StaleElementReferenceException) { return false; }
        });
    }

    protected IWebElement WaitForReRender(string previousSvgHtml, int timeoutSeconds = 15)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        // Wait for the SVG to change AND for makeNotesCollapsible() to finish adding
        // toggle icons. Under CPU load, there can be a gap between plantuml.js rendering
        // the new SVG and the JS callback that creates toggle icons on it.
        return wait.Until(d =>
        {
            try
            {
                var svg = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                if (svg.GetAttribute("outerHTML") == previousSvgHtml) return null;
                // Ensure makeNotesCollapsible has run — it always creates at least one
                // .note-toggle-icon per visible note (plus, minus, or arrow buttons).
                var icons = svg.FindElements(By.CssSelector(".note-toggle-icon"));
                return icons.Count > 0 ? svg : null;
            }
            catch { return null; }
        }) ?? throw new TimeoutException("SVG did not re-render with note toggle icons");
    }

    protected string GetSvgHtml()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        return wait.Until(d =>
        {
            try
            {
                return d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"))
                        .GetAttribute("outerHTML") ?? "";
            }
            catch (StaleElementReferenceException) { return null; }
        }) ?? "";
    }

    protected void SetScenarioState(string state)
    {
        var btn = _driver.FindElement(By.CssSelector(
            $".diagram-toggle .details-radio-btn[data-state='{state}']"));
        btn.Click();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        // Wait for both hover rects (note backgrounds) and toggle icons
        // (created by makeNotesCollapsible after re-render) to be present.
        wait.Until(d =>
            d.FindElements(By.CssSelector(".note-hover-rect")).Count > 0 &&
            d.FindElements(By.CssSelector(".note-toggle-icon")).Count > 0);
    }

    protected void DoubleClickFirstNoteAndWait()
    {
        var htmlBefore = GetSvgHtml();
        // Retry hoverRect lookup — SVG may still be replacing after a state change.
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            try
            {
                var hoverRect = d.FindElement(By.CssSelector(".note-hover-rect"));
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].dispatchEvent(new MouseEvent('dblclick', {bubbles:true, cancelable:true}));",
                    hoverRect);
                return true;
            }
            catch (StaleElementReferenceException) { return false; }
        });
        WaitForReRender(htmlBefore);
    }

    protected void ClickNoteButton(string cssSelector)
    {
        var htmlBefore = GetSvgHtml();
        HoverNoteRect(0);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            try
            {
                var btn = d.FindElement(By.CssSelector(cssSelector));
                return btn.GetCssValue("opacity") != "0";
            }
            catch (StaleElementReferenceException) { return false; }
        });

        // Use JS-dispatched click — native .Click() can be intercepted by SVG path elements.
        wait.Until(d =>
        {
            try
            {
                var btn = d.FindElement(By.CssSelector(cssSelector + " rect"));
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].dispatchEvent(new MouseEvent('click', {bubbles:true, cancelable:true}));",
                    btn);
                return true;
            }
            catch (StaleElementReferenceException) { return false; }
        });
        WaitForReRender(htmlBefore);
    }

    protected void ClickDownArrowAndWait()
    {
        var htmlBefore = GetSvgHtml();
        HoverNoteRect(0);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            try
            {
                var icons = d.FindElements(By.CssSelector(".note-toggle-icon"));
                return icons.Any(i =>
                {
                    var texts = i.FindElements(By.TagName("text"));
                    return texts.Any(t => t.Text.Contains("\u25BC")) && i.GetCssValue("opacity") != "0";
                });
            }
            catch (StaleElementReferenceException) { return false; }
        });

        // Use JS-dispatched click — native .Click() can be intercepted by SVG path elements.
        wait.Until(d =>
        {
            try
            {
                var downArrowGroup = d.FindElements(By.CssSelector(".note-toggle-icon"))
                    .First(i => i.FindElements(By.TagName("text")).Any(t => t.Text.Contains("\u25BC")));
                var rect = downArrowGroup.FindElement(By.TagName("rect"));
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].dispatchEvent(new MouseEvent('click', {bubbles:true, cancelable:true}));",
                    rect);
                return true;
            }
            catch (StaleElementReferenceException) { return false; }
        });
        WaitForReRender(htmlBefore);
    }

    protected string GeneratePartitionReport(string fileName) =>
        ReportTestHelper.GenerateReportWithPartitionDiagram(_tempDir, OutputDir, fileName);

    protected string GeneratePartitionLongNoteReport(string fileName) =>
        ReportTestHelper.GenerateReportWithPartitionLongNotes(_tempDir, OutputDir, fileName);

    protected string GenerateTwoLongNoteReport(string fileName) =>
        ReportTestHelper.GenerateReportWithTwoLongNoteDiagrams(_tempDir, OutputDir, fileName);

    protected string GenerateSplitDiagramReport(string fileName) =>
        ReportTestHelper.GenerateReportWithSplitDiagramLongNotes(_tempDir, OutputDir, fileName);

    protected string GenerateThreeDiagramSplitReport(string fileName) =>
        ReportTestHelper.GenerateReportWithThreeDiagramSplit(_tempDir, OutputDir, fileName);

    protected void RenderAllThreeDiagramsAndWait()
    {
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript("""
            document.querySelectorAll('[data-diagram-type="plantuml"]').forEach(function(c) {
                if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(c.parentElement);
            });
        """);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(120));
        // Use JS-based wait to avoid stale elements during re-render
        wait.Until(_ =>
        {
            try
            {
                return (bool)js.ExecuteScript("""
                    var svgs = document.querySelectorAll('[data-diagram-type="plantuml"] svg');
                    return svgs.length >= 3;
                """)!;
            }
            catch { return false; }
        });

        // Wait for all note processing to finish
        wait.Until(_ =>
        {
            try
            {
                return (bool)js.ExecuteScript("""
                    var containers = document.querySelectorAll('[data-diagram-type="plantuml"]');
                    for (var i = 0; i < containers.length; i++) {
                        if (containers[i]._noteRendering || window._plantumlRendering) return false;
                    }
                    return true;
                """)!;
            }
            catch { return false; }
        });
    }
}
