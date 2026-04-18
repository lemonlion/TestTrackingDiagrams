using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class ReportToolbarTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(ReportToolbarTests).Assembly.Location)!,
        "SeleniumOutput");

    public ReportToolbarTests()
    {
        _driver = ChromeDriverFactory.Create();
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-toolbar-" + Guid.NewGuid().ToString("N")[..8]);
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

    private string GetComputedStyle(IWebElement element, string property) =>
        (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return window.getComputedStyle(arguments[0]).getPropertyValue(arguments[1]);",
            element, property)!;

    // ── Expand / Collapse All buttons ──

    [Fact]
    public void Expand_all_features_opens_all_feature_details()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarExpandFeatures.html"));
        WaitFor(By.CssSelector("details.feature"));

        // Click "Expand All Features"
        var expandBtn = _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]"));
        expandBtn.Click();

        var features = _driver.FindElements(By.CssSelector("details.feature"));
        Assert.True(features.Count >= 2, "Should have at least 2 features");
        foreach (var f in features)
            Assert.NotNull(f.GetAttribute("open"));
    }

    [Fact]
    public void Collapse_all_features_closes_all_feature_details()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarCollapseFeatures.html"));
        WaitFor(By.CssSelector("details.feature"));

        // First expand, then collapse
        var btn = _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]"));
        btn.Click();
        Assert.Contains("Collapse", btn.Text);

        btn.Click();
        var features = _driver.FindElements(By.CssSelector("details.feature"));
        foreach (var f in features)
            Assert.Null(f.GetAttribute("open"));
    }

    [Fact]
    public void Expand_all_scenarios_opens_all_scenario_details()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarExpandScenarios.html"));
        WaitFor(By.CssSelector("details.feature"));

        // First expand features so scenarios are visible
        var expandFeatures = _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]"));
        expandFeatures.Click();

        var expandScenarios = _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]"));
        expandScenarios.Click();

        var scenarios = _driver.FindElements(By.CssSelector("details.scenario"));
        Assert.True(scenarios.Count >= 3, "Should have scenarios");
        foreach (var s in scenarios)
            Assert.NotNull(s.GetAttribute("open"));
    }

    [Fact]
    public void Collapse_all_scenarios_closes_all_scenario_details()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarCollapseScenarios.html"));
        WaitFor(By.CssSelector("details.feature"));

        // Expand features, then expand scenarios, then collapse scenarios
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        var btn = _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]"));
        btn.Click();
        Assert.Contains("Collapse", btn.Text);

        btn.Click();
        var scenarios = _driver.FindElements(By.CssSelector("details.scenario"));
        foreach (var s in scenarios)
            Assert.Null(s.GetAttribute("open"));
    }

    [Fact]
    public void Expand_collapse_button_text_toggles()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarToggleText.html"));
        WaitFor(By.CssSelector("details.feature"));

        var btn = _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]"));
        Assert.Contains("Expand", btn.Text);
        btn.Click();
        Assert.Contains("Collapse", btn.Text);
        btn.Click();
        Assert.Contains("Expand", btn.Text);
    }

    // ── Details radio buttons (Expanded / Collapsed / Truncated) ──

    [Fact]
    public void Truncated_is_active_by_default()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarTruncDefault.html"));
        WaitFor(By.CssSelector("details.feature"));

        var truncBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='truncated']"));
        Assert.Contains("details-active", truncBtn.GetAttribute("class")!);
    }

    [Fact]
    public void Clicking_expanded_activates_it_and_deactivates_truncated()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarExpandedRadio.html"));
        WaitFor(By.CssSelector("details.feature"));

        var expandedBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='expanded']"));
        var truncBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='truncated']"));

        expandedBtn.Click();
        Assert.Contains("details-active", expandedBtn.GetAttribute("class")!);
        Assert.DoesNotContain("details-active", truncBtn.GetAttribute("class")!);
    }

    [Fact]
    public void Clicking_collapsed_activates_it()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarCollapsedRadio.html"));
        WaitFor(By.CssSelector("details.feature"));

        var collapsedBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .details-radio-btn[data-state='collapsed']"));
        collapsedBtn.Click();
        Assert.Contains("details-active", collapsedBtn.GetAttribute("class")!);
    }

    // ── Truncate lines dropdown ──

    [Fact]
    public void Changing_line_count_syncs_all_dropdowns()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarLineSync.html"));
        WaitFor(By.CssSelector("details.feature"));

        // Expand features/scenarios so scenario-level dropdowns are visible
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]")).Click();

        var selects = _driver.FindElements(By.CssSelector(".truncate-lines-select"));
        Assert.True(selects.Count >= 2, "Need report-level + scenario-level dropdowns");

        // Change report-level dropdown
        var reportSelect = selects[0];
        var selectEl = new SelectElement(reportSelect);
        selectEl.SelectByValue("10");

        // All dropdowns should sync
        foreach (var sel in _driver.FindElements(By.CssSelector(".truncate-lines-select")))
        {
            var selectedOption = new SelectElement(sel).SelectedOption;
            Assert.Equal("10", selectedOption.GetAttribute("value"));
        }
    }

    // ── Headers shown / hidden ──

    [Fact]
    public void Headers_shown_is_active_by_default()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarHeaderDefault.html"));
        WaitFor(By.CssSelector("details.feature"));

        var shownBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='shown']"));
        Assert.Contains("details-active", shownBtn.GetAttribute("class")!);
    }

    [Fact]
    public void Clicking_hidden_activates_it_and_deactivates_shown()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarHiddenHeaders.html"));
        WaitFor(By.CssSelector("details.feature"));

        var hiddenBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='hidden']"));
        var shownBtn = _driver.FindElement(By.CssSelector(
            ".toolbar-row .headers-radio-btn[data-hstate='shown']"));

        hiddenBtn.Click();
        Assert.Contains("details-active", hiddenBtn.GetAttribute("class")!);
        Assert.DoesNotContain("details-active", shownBtn.GetAttribute("class")!);
    }

    // ── Search bar ──

    [Fact]
    public void Search_filters_scenarios_by_name()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarSearch.html"));
        WaitFor(By.CssSelector("details.feature"));

        var searchbar = _driver.FindElement(By.Id("searchbar"));
        searchbar.SendKeys("Delete");

        // Wait for debounce (300ms) + filter
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(_ =>
        {
            var passed = _driver.FindElements(By.CssSelector(".scenario[data-status='Passed']"));
            return passed.All(s => s.GetCssValue("display") == "none");
        });

        // "Delete order" scenario should still be visible (data-search is lowercased)
        var deleteScenario = _driver.FindElements(By.CssSelector(".scenario"))
            .FirstOrDefault(s => s.GetAttribute("data-search")?.Contains("delete") == true);
        Assert.NotNull(deleteScenario);
        Assert.NotEqual("none", deleteScenario.GetCssValue("display"));
    }

    [Fact]
    public void Search_single_match_auto_expands_scenario()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarSearchAutoExpand.html"));
        WaitFor(By.CssSelector("details.feature"));

        var searchbar = _driver.FindElement(By.Id("searchbar"));
        searchbar.SendKeys("Process payment");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        var scenario = wait.Until(_ =>
        {
            var matches = _driver.FindElements(By.CssSelector(".scenario"))
                .Where(s => s.GetCssValue("display") != "none")
                .ToList();
            return matches.Count == 1 ? matches[0] : null;
        });

        Assert.NotNull(scenario);
        // When only one match, scenario and its feature should auto-expand
        Assert.NotNull(scenario.GetAttribute("open"));
    }

    [Fact]
    public void Search_with_quoted_phrase_matches_scenarios()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarSearchQuoted.html"));
        WaitFor(By.CssSelector("details.feature"));

        var searchbar = _driver.FindElement(By.Id("searchbar"));
        // Search for "Delete order" with quotes — should match "Delete order fails gracefully"
        searchbar.SendKeys("\"Delete order\"");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        var matchingScenarios = wait.Until(_ =>
        {
            var visible = _driver.FindElements(By.CssSelector(".scenario"))
                .Where(s => s.GetCssValue("display") != "none")
                .ToList();
            // Wait for debounce to settle — we expect exactly 1 match
            return visible.Count <= 1 ? visible : null;
        });

        Assert.NotNull(matchingScenarios);
        Assert.Single(matchingScenarios);
        Assert.Contains("delete order", matchingScenarios[0].GetAttribute("data-search")!);
    }

    [Fact]
    public void Search_with_quoted_phrase_that_does_not_match_hides_all()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarSearchQuotedNoMatch.html"));
        WaitFor(By.CssSelector("details.feature"));

        var searchbar = _driver.FindElement(By.Id("searchbar"));
        // "order create" is not a contiguous phrase in any scenario name
        searchbar.SendKeys("\"order create\"");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(_ =>
        {
            var visible = _driver.FindElements(By.CssSelector(".scenario"))
                .Where(s => s.GetCssValue("display") != "none")
                .ToList();
            return visible.Count == 0;
        });

        var allHidden = _driver.FindElements(By.CssSelector(".scenario"))
            .All(s => s.GetCssValue("display") == "none");
        Assert.True(allHidden, "All scenarios should be hidden when quoted phrase doesn't match");
    }

    [Fact]
    public void Search_by_step_text_matches_scenario()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarSearchStepText.html"));
        WaitFor(By.CssSelector("details.feature"));

        var searchbar = _driver.FindElement(By.Id("searchbar"));
        // "non-existent" only appears in a step of "Delete order fails gracefully", not in any scenario name
        searchbar.SendKeys("non-existent");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        var matchingScenarios = wait.Until(_ =>
        {
            var visible = _driver.FindElements(By.CssSelector(".scenario"))
                .Where(s => s.GetCssValue("display") != "none")
                .ToList();
            return visible.Count == 1 ? visible : null;
        });

        Assert.NotNull(matchingScenarios);
        Assert.Single(matchingScenarios);
        Assert.Contains("delete order", matchingScenarios[0].GetAttribute("data-search")!);
    }

    // ── Clear All button ──

    [Fact]
    public void Clear_all_resets_search_and_status_filters()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarClearAll.html"));
        WaitFor(By.CssSelector("details.feature"));

        // Apply a search
        var searchbar = _driver.FindElement(By.Id("searchbar"));
        searchbar.SendKeys("Delete");

        // Apply a status filter
        var failedBtn = _driver.FindElement(By.CssSelector(".status-toggle[data-status='Failed']"));
        failedBtn.Click();

        // Click Clear All
        var clearAll = _driver.FindElement(By.XPath("//button[contains(text(),'Clear All')]"));
        clearAll.Click();

        // Search should be cleared
        Assert.Equal("", searchbar.GetAttribute("value"));

        // Status filter should be deactivated
        Assert.DoesNotContain("status-active", failedBtn.GetAttribute("class")!);

        // All scenarios should be visible
        var scenarios = _driver.FindElements(By.CssSelector(".scenario"));
        foreach (var s in scenarios)
            Assert.NotEqual("none", s.GetCssValue("display"));
    }

    // ── Scenario Timeline info icon ──

    [Fact]
    public void Timeline_info_icon_is_present_with_tooltip()
    {
        _driver.Navigate().GoToUrl(GenerateReport("ToolbarTimelineInfo.html"));
        WaitFor(By.CssSelector("details.feature"));

        // Open the timeline panel
        var toggleBtn = _driver.FindElement(By.CssSelector(".timeline-toggle"));
        toggleBtn.Click();

        var timeline = _driver.FindElement(By.Id("scenario-timeline"));
        Assert.NotEqual("none", timeline.GetCssValue("display"));

        // Verify the ⓘ icon exists inside the timeline header
        var infoIcon = timeline.FindElement(By.CssSelector(".timeline-info"));
        Assert.NotNull(infoIcon);

        // Verify it has a non-empty title attribute (the tooltip text)
        var tooltip = infoIcon.GetAttribute("title");
        Assert.False(string.IsNullOrWhiteSpace(tooltip));
        Assert.Contains("duration", tooltip, StringComparison.OrdinalIgnoreCase);
    }
}
