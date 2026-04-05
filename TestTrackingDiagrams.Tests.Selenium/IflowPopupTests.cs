using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class IflowPopupTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(IflowPopupTests).Assembly.Location)!,
        "SeleniumOutput");

    public IflowPopupTests()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1280,900");
        _driver = new ChromeDriver(options);
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-selenium-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    private string ServePage(string html, [System.Runtime.CompilerServices.CallerMemberName] string? testName = null)
    {
        var path = Path.Combine(_tempDir, "test.html");
        File.WriteAllText(path, html);

        // Persist a copy to the output directory for manual inspection
        var outputPath = Path.Combine(OutputDir, $"{testName}.html");
        File.WriteAllText(outputPath, html);

        return new Uri(path).AbsoluteUri;
    }

    private IWebElement WaitFor(By by, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private bool WaitUntilGone(By by, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d =>
        {
            try { return !d.FindElement(by).Displayed; }
            catch (NoSuchElementException) { return true; }
        });
    }

    /// <summary>
    /// Waits for the PlantUML browser JS to render the activity diagram SVG
    /// inside the popup's .iflow-diagram container. Returns the SVG element.
    /// </summary>
    private IWebElement WaitForActivityDiagramSvg(IWebElement popup, int timeoutSeconds = 15)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(_ =>
        {
            try
            {
                var svg = popup.FindElement(By.CssSelector(".iflow-diagram svg"));
                return svg.Displayed ? svg : null;
            }
            catch (NoSuchElementException) { return null; }
        }) ?? throw new TimeoutException("Activity diagram SVG did not render within timeout.");
    }

    /// <summary>
    /// Gets a computed CSS property value for an element via JavaScript.
    /// </summary>
    private string GetComputedStyle(IWebElement element, string property)
    {
        return (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return window.getComputedStyle(arguments[0]).getPropertyValue(arguments[1]);",
            element, property);
    }

    // ── Popup open/close ──

    [Fact]
    public void Clicking_trigger_opens_popup_overlay()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();

        var overlay = WaitFor(By.CssSelector(".iflow-overlay"));
        Assert.True(overlay.Displayed);

        // Verify overlay CSS is applied (fixed positioning, flex centering)
        Assert.Equal("fixed", GetComputedStyle(overlay, "position"));
        Assert.Equal("flex", GetComputedStyle(overlay, "display"));
        Assert.Equal("center", GetComputedStyle(overlay, "align-items"));

        var popup = _driver.FindElement(By.CssSelector(".iflow-popup"));
        Assert.True(popup.Displayed);

        // Verify popup CSS is applied (white background, border-radius)
        Assert.Equal("rgb(255, 255, 255)", GetComputedStyle(popup, "background-color"));
        Assert.Equal("8px", GetComputedStyle(popup, "border-radius"));

        // Verify the activity diagram actually rendered (not stuck on "Loading...")
        var svg = WaitForActivityDiagramSvg(popup);
        Assert.DoesNotContain("Loading", popup.FindElement(By.CssSelector(".iflow-diagram")).Text);
    }

    [Fact]
    public void Popup_shows_segment_title_and_content()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        var heading = popup.FindElement(By.TagName("h3"));
        Assert.Contains("Internal Flow", heading.Text);
        Assert.Contains("span", heading.Text);

        var diagram = popup.FindElement(By.CssSelector(".iflow-diagram"));
        Assert.True(diagram.Displayed);

        // Verify activity diagram SVG rendered with expected span operation names
        var svg = WaitForActivityDiagramSvg(popup);
        var svgText = svg.Text;
        Assert.Contains("HTTP GET /api/orders", svgText);
        Assert.Contains("SELECT", svgText);
    }

    [Fact]
    public void Close_button_removes_overlay()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        WaitFor(By.CssSelector(".iflow-overlay"));

        _driver.FindElement(By.CssSelector(".iflow-popup-close")).Click();

        Assert.True(WaitUntilGone(By.CssSelector(".iflow-overlay")));
    }

    [Fact]
    public void Escape_key_closes_popup()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        WaitFor(By.CssSelector(".iflow-overlay"));

        _driver.FindElement(By.TagName("body")).SendKeys(Keys.Escape);

        Assert.True(WaitUntilGone(By.CssSelector(".iflow-overlay")));
    }

    [Fact]
    public void Clicking_overlay_background_closes_popup()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        var overlay = WaitFor(By.CssSelector(".iflow-overlay"));

        // Click the overlay itself (not the popup) — use JS to click at the edge
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].click();", overlay);

        Assert.True(WaitUntilGone(By.CssSelector(".iflow-overlay")));
    }

    // ── Missing / empty segments ──

    [Fact]
    public void Missing_segment_shows_no_data_message()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-missing")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        var noData = popup.FindElement(By.CssSelector(".iflow-no-data"));
        Assert.True(noData.Displayed);
        Assert.Contains("No internal flow data", noData.Text);
    }

    [Fact]
    public void Empty_segment_shows_no_activity_message()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage(includeEmptySegment: true);
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-empty")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        var noData = popup.FindElement(By.CssSelector(".iflow-no-data"));
        Assert.True(noData.Displayed);
        Assert.Contains("No internal activity", noData.Text);
    }

    // ── Toggle buttons ──

    [Fact]
    public void Toggle_buttons_are_rendered_when_flame_chart_enabled()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true);
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        var toggleBtns = popup.FindElements(By.CssSelector(".iflow-toggle-btn"));
        Assert.Equal(2, toggleBtns.Count);
        Assert.Equal("Activity", toggleBtns[0].Text);
        Assert.Equal("Flame Chart", toggleBtns[1].Text);

        // Verify active button has styled background (blue) and inactive has default
        var activeBg = GetComputedStyle(toggleBtns[0], "background-color");
        var inactiveBg = GetComputedStyle(toggleBtns[1], "background-color");
        Assert.NotEqual(activeBg, inactiveBg);
        Assert.Equal("rgb(255, 255, 255)", GetComputedStyle(toggleBtns[0], "color"));
    }

    [Fact]
    public void Activity_view_is_visible_by_default_flame_is_hidden()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true);
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        var mainView = popup.FindElement(By.CssSelector(".iflow-view-main"));
        var flameView = popup.FindElement(By.CssSelector(".iflow-view-flame"));

        Assert.True(mainView.Displayed);
        Assert.False(flameView.Displayed);

        // Verify the activity diagram SVG actually rendered in the main view
        var svg = WaitForActivityDiagramSvg(popup);
        Assert.Contains("HTTP GET /api/orders", svg.Text);
    }

    [Fact]
    public void Clicking_flame_chart_toggle_shows_flame_hides_activity()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true);
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        // Wait for activity diagram to render first
        WaitForActivityDiagramSvg(popup);

        var toggleBtns = popup.FindElements(By.CssSelector(".iflow-toggle-btn"));
        toggleBtns[1].Click(); // "Flame Chart"

        var mainView = popup.FindElement(By.CssSelector(".iflow-view-main"));
        var flameView = popup.FindElement(By.CssSelector(".iflow-view-flame"));

        Assert.False(mainView.Displayed);
        Assert.True(flameView.Displayed);

        // Verify flame chart has real content with operation names
        var flameBars = flameView.FindElements(By.CssSelector(".iflow-flame-bar"));
        Assert.True(flameBars.Count >= 2, $"Expected at least 2 flame bars (parent + child), got {flameBars.Count}");
        var flameText = flameView.Text;
        Assert.Contains("HTTP GET /api/orders", flameText);
        Assert.Contains("SELECT", flameText);
    }

    [Fact]
    public void Clicking_activity_toggle_back_restores_activity_view()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true);
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        var toggleBtns = popup.FindElements(By.CssSelector(".iflow-toggle-btn"));
        toggleBtns[1].Click(); // Switch to Flame Chart
        toggleBtns[0].Click(); // Switch back to Activity

        var mainView = popup.FindElement(By.CssSelector(".iflow-view-main"));
        var flameView = popup.FindElement(By.CssSelector(".iflow-view-flame"));

        Assert.True(mainView.Displayed);
        Assert.False(flameView.Displayed);
    }

    [Fact]
    public void Active_toggle_button_has_active_class()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true);
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        var toggleBtns = popup.FindElements(By.CssSelector(".iflow-toggle-btn"));

        // Initially Activity is active
        Assert.Contains("iflow-toggle-active", toggleBtns[0].GetAttribute("class"));
        Assert.DoesNotContain("iflow-toggle-active", toggleBtns[1].GetAttribute("class"));

        // Click Flame Chart
        toggleBtns[1].Click();
        Assert.DoesNotContain("iflow-toggle-active", toggleBtns[0].GetAttribute("class"));
        Assert.Contains("iflow-toggle-active", toggleBtns[1].GetAttribute("class"));
    }

    // ── Flame chart content ──

    [Fact]
    public void Flame_chart_has_flame_bars()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true);
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        // Switch to flame chart view
        var toggleBtns = popup.FindElements(By.CssSelector(".iflow-toggle-btn"));
        toggleBtns[1].Click();

        var flameBars = popup.FindElements(By.CssSelector(".iflow-flame-bar"));
        Assert.True(flameBars.Count >= 2, $"Expected at least 2 flame bars (parent + child), got {flameBars.Count}");

        // Verify bars display operation names and have width styling
        var barTexts = flameBars.Select(b => b.Text).ToList();
        Assert.Contains(barTexts, t => t.Contains("HTTP GET /api/orders"));
        Assert.Contains(barTexts, t => t.Contains("SELECT"));
        Assert.All(flameBars, bar =>
            Assert.Contains("width", bar.GetAttribute("style")));

        // Verify flame bar CSS is applied (height, border-radius from stylesheet)
        Assert.All(flameBars, bar =>
        {
            Assert.Equal("22px", GetComputedStyle(bar, "height"));
            Assert.Equal("3px", GetComputedStyle(bar, "border-radius"));
        });
    }

    // ── Call tree ──

    [Fact]
    public void Call_tree_view_renders_nested_list()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage(includeCallTree: true);
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        var callTree = popup.FindElement(By.CssSelector(".iflow-call-tree"));
        Assert.True(callTree.Displayed);

        var items = callTree.FindElements(By.TagName("li"));
        Assert.True(items.Count >= 2, $"Expected at least 2 call tree items, got {items.Count}");

        // Verify items contain expected operation names
        var treeText = callTree.Text;
        Assert.Contains("HTTP GET /api/orders", treeText);
        Assert.Contains("SELECT", treeText);

        // Verify nesting: child span should be in a nested <ul>
        var nestedLists = callTree.FindElements(By.CssSelector("li > ul"));
        Assert.True(nestedLists.Count >= 1, "Expected nested list for child span");
    }

    // ── CSS applied correctly ──

    [Fact]
    public void Popup_styles_are_applied_from_stylesheet()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage(includeToggle: true);
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        // Overlay: fixed positioning, semi-transparent background
        var overlay = _driver.FindElement(By.CssSelector(".iflow-overlay"));
        Assert.Equal("fixed", GetComputedStyle(overlay, "position"));
        Assert.StartsWith("rgba(0, 0, 0, 0.", GetComputedStyle(overlay, "background-color"));
        Assert.Equal("20000", GetComputedStyle(overlay, "z-index"));

        // Popup: white background, rounded corners, box-shadow, padding
        Assert.Equal("rgb(255, 255, 255)", GetComputedStyle(popup, "background-color"));
        Assert.Equal("8px", GetComputedStyle(popup, "border-radius"));
        Assert.NotEqual("none", GetComputedStyle(popup, "box-shadow"));
        Assert.Equal("20px", GetComputedStyle(popup, "padding"));

        // Close button: absolute positioning
        var closeBtn = popup.FindElement(By.CssSelector(".iflow-popup-close"));
        Assert.Equal("absolute", GetComputedStyle(closeBtn, "position"));

        // Toggle buttons: styled with padding and border-radius
        var toggleBtns = popup.FindElements(By.CssSelector(".iflow-toggle-btn"));
        Assert.All(toggleBtns, btn => Assert.Equal("4px", GetComputedStyle(btn, "border-radius")));

        // Active toggle: blue background, white text
        var activeBtn = popup.FindElement(By.CssSelector(".iflow-toggle-active"));
        Assert.Equal("rgb(66, 133, 244)", GetComputedStyle(activeBtn, "background-color"));
        Assert.Equal("rgb(255, 255, 255)", GetComputedStyle(activeBtn, "color"));

        // Diagram container: has min-height from stylesheet
        var diagram = popup.FindElement(By.CssSelector(".iflow-diagram"));
        Assert.Equal("60px", GetComputedStyle(diagram, "min-height"));
    }

    // ── Multiple popups ──

    [Fact]
    public void Opening_new_popup_replaces_existing_one()
    {
        var html = TestPageGenerator.GenerateIflowPopupTestPage(includeEmptySegment: true);
        _driver.Navigate().GoToUrl(ServePage(html));

        // Open first popup
        _driver.FindElement(By.Id("trigger-seg-1")).Click();
        WaitFor(By.CssSelector(".iflow-popup"));

        // Open another via JS (overlay covers the page, so direct click is intercepted)
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "window._iflowShowPopup('iflow-seg-empty');");
        WaitFor(By.CssSelector(".iflow-no-data"));

        var overlays = _driver.FindElements(By.CssSelector(".iflow-overlay"));
        Assert.Single(overlays);
    }
}
