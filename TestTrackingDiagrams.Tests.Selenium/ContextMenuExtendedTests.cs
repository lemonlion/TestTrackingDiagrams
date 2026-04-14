using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class ContextMenuExtendedTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(ContextMenuExtendedTests).Assembly.Location)!,
        "SeleniumOutput");

    public ContextMenuExtendedTests()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1920,1080");
        _driver = new ChromeDriver(options);
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-ctxmenu-" + Guid.NewGuid().ToString("N")[..8]);
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

    private string GetComputedStyle(IWebElement el, string prop) =>
        (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return window.getComputedStyle(arguments[0]).getPropertyValue(arguments[1]);",
            el, prop)!;

    private void ExpandFirstScenarioWithDiagram()
    {
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]")).Click();
    }

    // ── Context menu positioning ──

    [Fact]
    public void Context_menu_z_index_is_high_enough()
    {
        _driver.Navigate().GoToUrl(GenerateReport("CtxZIndex.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        var menu = WaitFor(By.CssSelector(".diagram-ctx-menu"));

        var zIndex = GetComputedStyle(menu, "z-index");
        var zIndexVal = int.Parse(zIndex);
        Assert.True(zIndexVal >= 10000, $"Context menu z-index should be >= 10000 but was {zIndex}");
    }

    [Fact]
    public void Context_menu_has_box_shadow()
    {
        _driver.Navigate().GoToUrl(GenerateReport("CtxShadow.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        var menu = WaitFor(By.CssSelector(".diagram-ctx-menu"));

        var boxShadow = GetComputedStyle(menu, "box-shadow");
        Assert.NotEqual("none", boxShadow);
    }

    // ── Submenu structure ──

    [Fact]
    public void Copy_image_submenu_has_submenu_children()
    {
        _driver.Navigate().GoToUrl(GenerateReport("CtxSubmenuChildren.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        WaitFor(By.CssSelector(".diagram-ctx-menu"));

        var submenuParents = _driver.FindElements(By.CssSelector(".diagram-ctx-menu .submenu-parent"));
        Assert.True(submenuParents.Count >= 3,
            $"Should have at least 3 submenu parents (Copy/Save/Open) but found {submenuParents.Count}");

        // Each submenu parent should have a .submenu child
        foreach (var parent in submenuParents)
        {
            var submenu = parent.FindElements(By.CssSelector(".submenu"));
            Assert.True(submenu.Count == 1, $"Submenu parent '{parent.Text.Split('\n')[0]}' should have exactly 1 submenu");
        }
    }

    [Fact]
    public void Submenu_is_hidden_by_default()
    {
        _driver.Navigate().GoToUrl(GenerateReport("CtxSubmenuHidden.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        WaitFor(By.CssSelector(".diagram-ctx-menu"));

        var submenu = _driver.FindElement(By.CssSelector(".diagram-ctx-menu .submenu"));
        Assert.Equal("none", GetComputedStyle(submenu, "display"));
    }

    [Fact]
    public void Submenu_shows_on_parent_hover()
    {
        _driver.Navigate().GoToUrl(GenerateReport("CtxSubmenuHover.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        WaitFor(By.CssSelector(".diagram-ctx-menu"));

        var parent = _driver.FindElement(By.CssSelector(".diagram-ctx-menu .submenu-parent"));
        new Actions(_driver).MoveToElement(parent).Perform();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(_ =>
        {
            var submenu = parent.FindElement(By.CssSelector(".submenu"));
            return GetComputedStyle(submenu, "display") != "none";
        });
    }

    [Fact]
    public void Submenu_parent_has_arrow_indicator()
    {
        _driver.Navigate().GoToUrl(GenerateReport("CtxSubmenuArrow.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        WaitFor(By.CssSelector(".diagram-ctx-menu"));

        var parent = _driver.FindElement(By.CssSelector(".diagram-ctx-menu .submenu-parent"));
        // The ::after pseudo-element has the arrow. Check computed content via JS
        var content = (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return window.getComputedStyle(arguments[0], '::after').content;", parent)!;
        Assert.False(string.IsNullOrEmpty(content) || content == "none",
            "Submenu parent should have ::after arrow indicator");
    }

    // ── Show Browser Menu item ──

    [Fact]
    public void Context_menu_has_show_browser_menu_item()
    {
        _driver.Navigate().GoToUrl(GenerateReport("CtxBrowserMenu.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        var menu = WaitFor(By.CssSelector(".diagram-ctx-menu"));

        var items = menu.FindElements(By.CssSelector(":scope > div"))
            .Select(i => i.Text.Split('\n')[0].Trim())
            .ToList();

        Assert.Contains("Show default browser menu", items);
    }

    // ── Copy PlantUML source item ──

    [Fact]
    public void Context_menu_has_copy_source_item()
    {
        _driver.Navigate().GoToUrl(GenerateReport("CtxCopySource.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        var menu = WaitFor(By.CssSelector(".diagram-ctx-menu"));

        var items = menu.FindElements(By.CssSelector(":scope > div"))
            .Select(i => i.Text.Split('\n')[0].Trim())
            .ToList();

        // Should have either "Copy PlantUML source" (non-submenu) or a submenu containing it
        var hasSourceItem = items.Any(t =>
            t.Contains("Copy") && t.Contains("source"));
        Assert.True(hasSourceItem,
            $"Expected a 'Copy ... source' item in context menu. Items: {string.Join(", ", items)}");
    }

    // ── Context menu replaced on new right-click ──

    [Fact]
    public void New_right_click_replaces_existing_context_menu()
    {
        _driver.Navigate().GoToUrl(GenerateReport("CtxReplace.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        WaitFor(By.CssSelector(".diagram-ctx-menu"));

        // Right-click again in a different position
        new Actions(_driver).MoveToElement(svg, 10, 10).ContextClick().Perform();

        // Should still have exactly one menu
        var menus = _driver.FindElements(By.CssSelector(".diagram-ctx-menu"));
        Assert.Equal(1, menus.Count);
    }

    // ── Open in new tab items ──

    [Fact]
    public void Context_menu_has_open_in_new_tab_submenu()
    {
        _driver.Navigate().GoToUrl(GenerateReport("CtxOpenTab.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        var menu = WaitFor(By.CssSelector(".diagram-ctx-menu"));

        var items = menu.FindElements(By.CssSelector(":scope > div"))
            .Select(i => i.Text.Split('\n')[0].Trim())
            .Where(t => t.Length > 0)
            .ToList();

        Assert.Contains("Open image in new tab", items);
    }

    // ── Separator exists in menu ──

    [Fact]
    public void Context_menu_has_separator()
    {
        _driver.Navigate().GoToUrl(GenerateReport("CtxSeparator.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        WaitFor(By.CssSelector(".diagram-ctx-menu"));

        // Separators are <hr> elements
        var separators = _driver.FindElements(By.CssSelector(".diagram-ctx-menu > hr"));
        Assert.True(separators.Count >= 1, "Context menu should have at least one separator");
    }
}
