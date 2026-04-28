using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class ScenarioInteractionTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(ScenarioInteractionTests).Assembly.Location)!,
        "SeleniumOutput");

    public ScenarioInteractionTests()
    {
        _driver = ChromeDriverFactory.Create();
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-scenario-" + Guid.NewGuid().ToString("N")[..8]);
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

    private void ExpandFirstScenarioWithDiagram()
    {
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]")).Click();
    }

    // ── Feature and scenario expand/collapse via click ──

    [Fact]
    public void Clicking_feature_summary_opens_feature()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioFeatureOpen.html"));
        WaitFor(By.CssSelector("details.feature"));

        var feature = _driver.FindElement(By.CssSelector("details.feature"));
        Assert.Null(feature.GetAttribute("open"));

        feature.FindElement(By.CssSelector("summary")).Click();
        Assert.NotNull(feature.GetAttribute("open"));
    }

    [Fact]
    public void Clicking_scenario_summary_opens_scenario()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioOpen.html"));
        WaitFor(By.CssSelector("details.feature"));

        // Open the feature first
        _driver.FindElement(By.CssSelector("details.feature summary")).Click();

        var scenario = WaitFor(By.CssSelector("details.scenario"));
        Assert.Null(scenario.GetAttribute("open"));

        scenario.FindElement(By.CssSelector("summary")).Click();
        Assert.NotNull(scenario.GetAttribute("open"));
    }

    // ── Copy scenario name button ──

    [Fact]
    public void Copy_scenario_name_button_exists_on_each_scenario()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioCopyBtn.html"));
        WaitFor(By.CssSelector("details.feature"));

        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();

        var copyBtns = _driver.FindElements(By.CssSelector(".copy-scenario-name"));
        var scenarios = _driver.FindElements(By.CssSelector("details.scenario"));
        Assert.Equal(scenarios.Count, copyBtns.Count);
    }

    [Fact]
    public void Copy_scenario_name_button_shows_checkmark_after_click()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioCopyCheck.html"));
        WaitFor(By.CssSelector("details.feature"));

        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();

        var copyBtn = _driver.FindElement(By.CssSelector(".copy-scenario-name"));
        var origText = copyBtn.Text;
        copyBtn.Click();

        // Should briefly show ✓
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(2));
        wait.Until(_ => copyBtn.Text == "\u2713");
        Assert.Equal("\u2713", copyBtn.Text);
    }

    [Fact]
    public void Copy_scenario_name_button_reverts_after_delay()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioCopyRevert.html"));
        WaitFor(By.CssSelector("details.feature"));

        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();

        var copyBtn = _driver.FindElement(By.CssSelector(".copy-scenario-name"));
        var origText = copyBtn.Text;
        copyBtn.Click();

        // First wait for the checkmark to appear (clipboard is async)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(_ => copyBtn.Text == "\u2713");

        // Then wait for revert (1500ms timeout in JS)
        wait.Until(_ => copyBtn.Text == origText);
        Assert.Equal(origText, copyBtn.Text);
    }

    [Fact]
    public void Copy_scenario_name_has_correct_data_attribute()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioCopyData.html"));
        WaitFor(By.CssSelector("details.feature"));

        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();

        var copyBtn = _driver.FindElement(By.CssSelector(".copy-scenario-name"));
        var name = copyBtn.GetAttribute("data-scenario-name");
        Assert.False(string.IsNullOrEmpty(name), "data-scenario-name should not be empty");
    }

    // ── Scenario link button ──

    [Fact]
    public void Scenario_link_exists_on_each_scenario()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioLink.html"));
        WaitFor(By.CssSelector("details.feature"));

        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();

        var links = _driver.FindElements(By.CssSelector(".scenario-link"));
        var scenarios = _driver.FindElements(By.CssSelector("details.scenario"));
        Assert.Equal(scenarios.Count, links.Count);
    }

    [Fact]
    public void Scenario_link_href_points_to_scenario_id()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioLinkHref.html"));
        WaitFor(By.CssSelector("details.feature"));

        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();

        var link = _driver.FindElement(By.CssSelector(".scenario-link"));
        var href = link.GetAttribute("href");
        Assert.Contains("#scenario-", href);
    }

    // ── Duration badge ──

    [Fact]
    public void Duration_badge_shows_time_for_scenarios_with_duration()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioDuration.html"));
        WaitFor(By.CssSelector("details.feature"));

        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();

        var badges = _driver.FindElements(By.CssSelector(".duration-badge"));
        Assert.True(badges.Count > 0, "Duration badges should exist");
        Assert.True(badges.Any(b => !string.IsNullOrWhiteSpace(b.Text)),
            "At least one duration badge should have text");
    }

    // ── Scenario status classes ──

    [Fact]
    public void Scenarios_have_correct_status_data_attribute()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioStatus.html"));
        WaitFor(By.CssSelector("details.feature"));

        var passed = _driver.FindElements(By.CssSelector(".scenario[data-status='Passed']"));
        var failed = _driver.FindElements(By.CssSelector(".scenario[data-status='Failed']"));
        var skipped = _driver.FindElements(By.CssSelector(".scenario[data-status='Skipped']"));

        Assert.True(passed.Count >= 2, "Should have at least 2 passed scenarios");
        Assert.True(failed.Count >= 1, "Should have at least 1 failed scenario");
        Assert.True(skipped.Count >= 1, "Should have at least 1 skipped scenario");
    }

    // ── Happy path class ──

    [Fact]
    public void Happy_path_scenarios_have_class()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioHappyPath.html"));
        WaitFor(By.CssSelector("details.feature"));

        var happyPaths = _driver.FindElements(By.CssSelector(".scenario.happy-path"));
        Assert.True(happyPaths.Count >= 2, "Should have at least 2 happy path scenarios");
    }

    // ── Steps rendering ──

    [Fact]
    public void Scenario_steps_render_inside_open_scenario()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioSteps.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var steps = _driver.FindElements(By.CssSelector(".scenario-steps .step"));
        Assert.True(steps.Count >= 3, "First scenario should have at least 3 steps");
    }

    [Fact]
    public void Steps_section_is_collapsible_details_element()
    {
        _driver.Navigate().GoToUrl(GenerateReport("StepsCollapsible.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var stepsDetails = _driver.FindElement(By.CssSelector("details.scenario-steps"));
        Assert.NotNull(stepsDetails.GetAttribute("open"));

        var summary = stepsDetails.FindElement(By.CssSelector("summary"));
        Assert.Contains("Steps", summary.Text);
    }

    [Fact]
    public void Steps_section_can_be_collapsed_by_clicking_summary()
    {
        _driver.Navigate().GoToUrl(GenerateReport("StepsCollapse.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var stepsDetails = _driver.FindElement(By.CssSelector("details.scenario-steps"));
        Assert.NotNull(stepsDetails.GetAttribute("open"));

        stepsDetails.FindElement(By.CssSelector("summary")).Click();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(d =>
        {
            var el = d.FindElement(By.CssSelector("details.scenario-steps"));
            return el.GetAttribute("open") == null;
        });

        Assert.Null(stepsDetails.GetAttribute("open"));
    }

    [Fact]
    public void Steps_section_has_rounded_border()
    {
        _driver.Navigate().GoToUrl(GenerateReport("StepsBorder.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var stepsDetails = _driver.FindElement(By.CssSelector("details.scenario-steps"));
        var borderWidth = stepsDetails.GetCssValue("border-width");
        Assert.Equal("1px", borderWidth);
        var borderRadius = stepsDetails.GetCssValue("border-radius");
        Assert.Contains("16px", borderRadius); // 1em = 16px at default font size
    }

    // ── Diagram container renders ──

    [Fact]
    public void Plantuml_browser_diagram_renders_svg()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioDiagramRender.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();

        var svg = WaitForDiagramSvg();
        Assert.True(svg.Displayed, "SVG should be visible");
    }

    // ── Right-click context menu on rendered diagram ──

    [Fact]
    public void Right_click_on_diagram_shows_context_menu()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioCtxMenu.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();

        var menu = WaitFor(By.CssSelector(".diagram-ctx-menu"));
        Assert.True(menu.Displayed, "Context menu should appear on right-click");
    }

    [Fact]
    public void Context_menu_has_copy_and_save_submenus()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioCtxMenuItems.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();

        var menu = WaitFor(By.CssSelector(".diagram-ctx-menu"));
        var topItems = menu.FindElements(By.CssSelector(":scope > div"))
            .Select(i => i.Text.Split('\n')[0].Trim())
            .Where(t => t.Length > 0)
            .ToList();

        Assert.Contains("Copy image", topItems);
        Assert.Contains("Save image", topItems);
    }

    [Fact]
    public void Context_menu_dismissed_by_clicking_elsewhere()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioCtxMenuDismiss.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        WaitFor(By.CssSelector(".diagram-ctx-menu"));

        // Click elsewhere to dismiss
        _driver.FindElement(By.TagName("body")).Click();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(d =>
        {
            try { return !d.FindElement(By.CssSelector(".diagram-ctx-menu")).Displayed; }
            catch (NoSuchElementException) { return true; }
        });
    }

    [Fact]
    public void Context_menu_dismissed_by_escape_key()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioCtxMenuEsc.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFirstScenarioWithDiagram();
        var svg = WaitForDiagramSvg();

        new Actions(_driver).ContextClick(svg).Perform();
        WaitFor(By.CssSelector(".diagram-ctx-menu"));

        new Actions(_driver).SendKeys(Keys.Escape).Perform();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(d =>
        {
            try { return !d.FindElement(By.CssSelector(".diagram-ctx-menu")).Displayed; }
            catch (NoSuchElementException) { return true; }
        });
    }

    // ── Status filter buttons ──

    [Fact]
    public void Clicking_status_filter_hides_non_matching_scenarios()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioStatusFilter.html"));
        WaitFor(By.CssSelector("details.feature"));

        var passedBtn = _driver.FindElement(By.CssSelector(".status-toggle[data-status='Passed']"));
        passedBtn.Click();

        // Wait for filter to apply
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(_ =>
        {
            var failed = _driver.FindElements(By.CssSelector(".scenario[data-status='Failed']"));
            return failed.All(s => s.GetCssValue("display") == "none");
        });

        // Passed scenarios should remain visible
        var passedScenarios = _driver.FindElements(By.CssSelector(".scenario[data-status='Passed']"));
        Assert.Contains(passedScenarios, s => s.GetCssValue("display") != "none");
    }

    [Fact]
    public void Clicking_status_filter_again_deactivates_it()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioStatusToggle.html"));
        WaitFor(By.CssSelector("details.feature"));

        var passedBtn = _driver.FindElement(By.CssSelector(".status-toggle[data-status='Passed']"));
        passedBtn.Click();
        Assert.Contains("status-active", passedBtn.GetAttribute("class")!);

        passedBtn.Click();
        Assert.DoesNotContain("status-active", passedBtn.GetAttribute("class")!);

        // All scenarios should be visible again
        var allScenarios = _driver.FindElements(By.CssSelector(".scenario"));
        foreach (var s in allScenarios)
            Assert.NotEqual("none", s.GetCssValue("display"));
    }

    // ── Happy path filter ──

    [Fact]
    public void Clicking_happy_path_filter_hides_non_happy_scenarios()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioHappyFilter.html"));
        WaitFor(By.CssSelector("details.feature"));

        var hpBtn = _driver.FindElement(By.CssSelector(".happy-path-toggle"));
        hpBtn.Click();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(_ =>
        {
            var nonHappy = _driver.FindElements(By.CssSelector(".scenario:not(.happy-path)"));
            return nonHappy.All(s => s.GetCssValue("display") == "none");
        });

        Assert.Contains("happy-path-active", hpBtn.GetAttribute("class")!);
    }

    // ── Category filter ──

    [Fact]
    public void Category_buttons_render_for_scenarios_with_categories()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ScenarioCategoryBtns.html"));
        WaitFor(By.CssSelector("details.feature"));

        var catBtns = _driver.FindElements(By.CssSelector(".category-toggle"));
        Assert.True(catBtns.Count >= 2,
            $"Should have category buttons but found {catBtns.Count}");
    }
}
