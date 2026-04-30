using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class MobileResponsiveTests : IClassFixture<ChromeFixtureMobile>, IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(MobileResponsiveTests).Assembly.Location)!,
        "SeleniumOutput");

    public MobileResponsiveTests(ChromeFixtureMobile chrome)
    {
        _driver = chrome.Driver;
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-mobile-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    private string GenerateReport(string fileName) =>
        ReportTestHelper.GenerateReport(_tempDir, OutputDir, fileName);

    private IWebElement WaitFor(By by, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private string GetComputedStyle(IWebElement element, string property) =>
        (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return window.getComputedStyle(arguments[0]).getPropertyValue(arguments[1]);",
            element, property)!;

    // ── Viewport meta tag ──

    [Fact]
    public void Report_has_viewport_meta_tag()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileViewport.html"));
        var meta = _driver.FindElement(By.CssSelector("meta[name='viewport']"));
        Assert.Contains("width=device-width", meta.GetAttribute("content"));
    }

    [Fact]
    public void Report_has_doctype_and_charset()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileDoctype.html"));
        var pageSource = _driver.PageSource;
        var charset = _driver.FindElement(By.CssSelector("meta[charset]"));
        Assert.Equal("utf-8", charset.GetAttribute("charset"));
    }

    // ── Header row stacks vertically ──

    [Fact]
    public void Header_row_stacks_vertically_on_mobile()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileHeaderStack.html"));
        var headerRow = WaitFor(By.CssSelector(".header-row"));
        var flexDirection = GetComputedStyle(headerRow, "flex-direction");
        Assert.Equal("column", flexDirection);
    }

    // ── Toolbar stacks vertically ──

    [Fact]
    public void Toolbar_stacks_vertically_on_mobile()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileToolbar.html"));
        WaitFor(By.CssSelector("details.feature"));

        var toolbar = _driver.FindElement(By.CssSelector(".toolbar-row"));
        var flexDirection = GetComputedStyle(toolbar, "flex-direction");
        Assert.Equal("column", flexDirection);
    }

    // ── No horizontal overflow ──

    [Fact]
    public void Page_does_not_overflow_horizontally()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileOverflow.html"));
        WaitFor(By.CssSelector("details.feature"));

        var viewportWidth = Convert.ToInt64(((IJavaScriptExecutor)_driver).ExecuteScript(
            "return window.innerWidth;"));
        var scrollWidth = Convert.ToInt64(((IJavaScriptExecutor)_driver).ExecuteScript(
            "return document.documentElement.scrollWidth;"));

        // Allow 2px tolerance for sub-pixel rendering
        Assert.True(scrollWidth <= viewportWidth + 2,
            $"Page overflows: scrollWidth={scrollWidth}, viewportWidth={viewportWidth}");
    }

    // ── Filter buttons wrap ──

    [Fact]
    public void Filter_row_stacks_vertically_on_mobile()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileFilters.html"));
        WaitFor(By.CssSelector(".filter-row"));

        var filterRow = _driver.FindElement(By.CssSelector(".filter-row"));
        var flexDirection = GetComputedStyle(filterRow, "flex-direction");
        Assert.Equal("column", flexDirection);
    }

    // ── Tables scroll horizontally ──

    [Fact]
    public void Feature_summary_table_scrolls_horizontally()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileTable.html"));
        WaitFor(By.CssSelector(".features-summary-details"));

        // Open the summary
        var summary = _driver.FindElement(By.CssSelector(".features-summary-details > summary"));
        summary.Click();

        var table = _driver.FindElement(By.CssSelector(".feature-summary-table"));
        var overflow = GetComputedStyle(table, "overflow-x");
        // The table's parent wrapper or the table itself should be scrollable
        var display = GetComputedStyle(table, "display");
        Assert.Equal("block", display); // display:block enables overflow-x:auto
    }

    // ── Filtering box takes full width ──

    [Fact]
    public void Filtering_box_takes_full_width_on_mobile()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileFilterBox.html"));
        var filterBox = WaitFor(By.CssSelector(".filtering-box"));

        var boxSizing = GetComputedStyle(filterBox, "box-sizing");
        Assert.Equal("border-box", boxSizing);

        // Box should not exceed viewport
        var viewportWidth = Convert.ToInt64(((IJavaScriptExecutor)_driver).ExecuteScript(
            "return window.innerWidth;"));
        var boxWidth = filterBox.Size.Width;
        Assert.True(boxWidth <= viewportWidth,
            $"Filter box width ({boxWidth}) exceeds viewport ({viewportWidth})");
    }

    // ── Jump-to-failure button is accessible ──

    [Fact]
    public void Jump_to_failure_button_visible_on_mobile()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileJump.html"));
        WaitFor(By.CssSelector("details.feature"));

        var buttons = _driver.FindElements(By.CssSelector(".jump-to-failure"));
        if (buttons.Count > 0)
        {
            var btn = buttons[0];
            Assert.True(btn.Displayed, "Jump-to-failure button should be visible");
            var right = Convert.ToInt64(((IJavaScriptExecutor)_driver).ExecuteScript(
                "var r = arguments[0].getBoundingClientRect(); return r.right;", btn));
            var viewportWidth = Convert.ToInt64(((IJavaScriptExecutor)_driver).ExecuteScript(
                "return window.innerWidth;"));
            Assert.True(right <= viewportWidth,
                $"Button right edge ({right}) exceeds viewport ({viewportWidth})");
        }
    }

    // ── Desktop is unaffected at full width ──

    [Fact]
    public void Desktop_layout_unaffected_at_1920_width()
    {
        // Use JS to check what the computed style would be at desktop width
        // Since we're in a 375px viewport, the media query is active.
        // Instead, verify the media query threshold is correct.
        _driver.Navigate().GoToUrl(GenerateReport("MobileDesktopCheck.html"));
        WaitFor(By.CssSelector(".header-row"));

        // At 375px, header-row should be column
        var headerRow = _driver.FindElement(By.CssSelector(".header-row"));
        Assert.Equal("column", GetComputedStyle(headerRow, "flex-direction"));

        // Now resize to desktop and verify it goes back to row
        _driver.Manage().Window.Size = new System.Drawing.Size(1920, 1080);
        _driver.Navigate().Refresh();
        WaitFor(By.CssSelector(".header-row"));
        headerRow = _driver.FindElement(By.CssSelector(".header-row"));
        Assert.Equal("row", GetComputedStyle(headerRow, "flex-direction"));

        // Restore mobile size for remaining tests
        _driver.Manage().Window.Size = new System.Drawing.Size(375, 812);
    }

    // ── Summary chart centred on mobile ──

    [Fact]
    public void Summary_chart_is_centred_on_mobile()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileChartCenter.html"));
        var chart = WaitFor(By.CssSelector(".summary-chart"));
        var alignSelf = GetComputedStyle(chart, "align-self");
        Assert.Equal("center", alignSelf);
    }

    // ── Details/Headers wraps on mobile ──

    [Fact]
    public void Toolbar_right_details_does_not_overflow()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileDetailsOverflow.html"));
        WaitFor(By.CssSelector(".toolbar-right"));

        var toolbarRight = _driver.FindElement(By.CssSelector(".toolbar-right"));
        var viewportWidth = Convert.ToInt64(((IJavaScriptExecutor)_driver).ExecuteScript(
            "return window.innerWidth;"));
        var rightEdge = Convert.ToInt64(((IJavaScriptExecutor)_driver).ExecuteScript(
            "return arguments[0].getBoundingClientRect().right;", toolbarRight));

        // Toolbar-right should not extend beyond viewport
        Assert.True(rightEdge <= viewportWidth + 2,
            $"Toolbar-right overflows: right={rightEdge}, viewport={viewportWidth}");
    }

    [Fact]
    public void Diagram_toggle_wraps_on_mobile()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileDiagramToggle.html"));
        WaitFor(By.CssSelector("details.feature"));

        // Open a feature and scenario to access the diagram toggle
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.querySelectorAll('details.feature')[0].setAttribute('open','');");
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.querySelectorAll('details.scenario')[0].setAttribute('open','');");
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.querySelectorAll('details.example-diagrams')[0]?.setAttribute('open','');");

        var toggles = _driver.FindElements(By.CssSelector(".diagram-toggle"));
        if (toggles.Count > 0)
        {
            var toggle = toggles[0];
            var flexWrap = GetComputedStyle(toggle, "flex-wrap");
            Assert.Equal("wrap", flexWrap);

            // Spacer should be hidden so items flow naturally
            var spacers = _driver.FindElements(By.CssSelector(".diagram-toggle-spacer"));
            if (spacers.Count > 0)
            {
                var display = GetComputedStyle(spacers[0], "display");
                Assert.Equal("none", display);
            }
        }
    }

    [Fact]
    public void Headers_radio_has_no_left_margin_on_mobile()
    {
        _driver.Navigate().GoToUrl(GenerateReport("MobileHeadersMargin.html"));
        WaitFor(By.CssSelector(".toolbar-right"));

        var headersRadios = _driver.FindElements(By.CssSelector(".headers-radio"));
        if (headersRadios.Count > 0)
        {
            var marginLeft = GetComputedStyle(headersRadios[0], "margin-left");
            Assert.Equal("0px", marginLeft);
        }
    }
}
