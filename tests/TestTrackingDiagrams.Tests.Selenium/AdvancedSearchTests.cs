using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class AdvancedSearchTests : IClassFixture<ChromeFixture>, IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(AdvancedSearchTests).Assembly.Location)!,
        "SeleniumOutput");

    public AdvancedSearchTests(ChromeFixture chrome)
    {
        _driver = chrome.Driver;
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-advsearch-" + Guid.NewGuid().ToString("N")[..8]);
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

    private List<IWebElement> GetVisibleScenarios()
    {
        return _driver.FindElements(By.CssSelector(".scenario"))
            .Where(s => s.GetCssValue("display") != "none")
            .ToList();
    }

    private void SearchAndWait(string query, Func<bool> condition)
    {
        var searchbar = _driver.FindElement(By.Id("searchbar"));
        searchbar.Clear();
        searchbar.SendKeys(query);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(_ => condition());
    }

    // ── AND operator (&&) ──

    [Fact]
    public void And_operator_matches_scenarios_containing_both_terms()
    {
        // Test data: t1="Create order successfully" (Passed, Smoke+API),
        //            t2="Delete order fails gracefully" (Failed, API),
        //            t3="List orders returns paginated results" (Passed, Smoke)
        //            t4="Process payment" (Passed), t5="Refund payment" (Skipped)
        // "order && successfully" should match only t1 ("Create order successfully")
        // Note: data-search includes diagram source, so use display-name-only terms
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchAnd.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("order && successfully", () => GetVisibleScenarios().Count == 1);

        var visible = GetVisibleScenarios();
        Assert.Single(visible);
        Assert.Contains("create order", visible[0].GetAttribute("data-search")!);
    }

    [Fact]
    public void And_operator_hides_all_when_no_scenario_matches_both_terms()
    {
        // "payment && order" — no scenario contains both
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchAndNoMatch.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("payment && order", () => GetVisibleScenarios().Count == 0);

        Assert.Empty(GetVisibleScenarios());
    }

    // ── OR operator (||) ──

    [Fact]
    public void Or_operator_matches_scenarios_containing_either_term()
    {
        // "payment || delete" should match t2 (delete), t4 (payment), t5 (payment)
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchOr.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("payment || delete", () =>
        {
            var vis = GetVisibleScenarios();
            return vis.Count == 3 &&
                   vis.All(s => s.GetAttribute("data-search")!.Contains("payment") ||
                                s.GetAttribute("data-search")!.Contains("delete"));
        });

        var visible = GetVisibleScenarios();
        Assert.Equal(3, visible.Count);
    }

    // ── NOT operator (!!) ──

    [Fact]
    public void Not_operator_excludes_matching_scenarios()
    {
        // "order && !!delete" should match t1 (create order) and t3 (list orders) but not t2 (delete order)
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchNot.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("order && !!delete", () =>
        {
            var vis = GetVisibleScenarios();
            return vis.Count == 2 &&
                   vis.All(s => !s.GetAttribute("data-search")!.Contains("delete"));
        });

        var visible = GetVisibleScenarios();
        Assert.Equal(2, visible.Count);
        Assert.All(visible, s =>
        {
            Assert.Contains("order", s.GetAttribute("data-search")!);
            Assert.DoesNotContain("delete", s.GetAttribute("data-search")!);
        });
    }

    // ── $status filter ──

    [Fact]
    public void Status_filter_matches_scenarios_by_status()
    {
        // "$failed" should match only t2 (Failed)
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchStatus.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("$failed", () => GetVisibleScenarios().Count == 1);

        var visible = GetVisibleScenarios();
        Assert.Single(visible);
        Assert.Equal("Failed", visible[0].GetAttribute("data-status"));
    }

    [Fact]
    public void Status_filter_combined_with_text_using_and()
    {
        // "$passed && order" should match t1 (Passed + order), t3 (Passed + order) but not t2 (Failed)
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchStatusAnd.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("$passed && order", () => GetVisibleScenarios().Count == 2);

        var visible = GetVisibleScenarios();
        Assert.Equal(2, visible.Count);
        Assert.All(visible, s =>
        {
            Assert.Equal("Passed", s.GetAttribute("data-status"));
            Assert.Contains("order", s.GetAttribute("data-search")!);
        });
    }

    // ── @tag filter ──

    [Fact]
    public void Tag_filter_matches_scenarios_by_category()
    {
        // "@smoke" should match t1 (Smoke+API) and t3 (Smoke)
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchTag.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("@smoke", () => GetVisibleScenarios().Count == 2);

        var visible = GetVisibleScenarios();
        Assert.Equal(2, visible.Count);
        Assert.All(visible, s =>
            Assert.Contains("Smoke", s.GetAttribute("data-categories")!));
    }

    [Fact]
    public void Tag_with_and_operator_narrows_results()
    {
        // "@api && @smoke" should match only t1 (has both Smoke and API)
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchTagAnd.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("@api && @smoke", () => GetVisibleScenarios().Count == 1);

        var visible = GetVisibleScenarios();
        Assert.Single(visible);
        Assert.Contains("create order", visible[0].GetAttribute("data-search")!);
    }

    // ── Parenthesized grouping ──

    [Fact]
    public void Parentheses_change_evaluation_order()
    {
        // "payment || (order && delete)" should match:
        //   - t4 ("Process payment") and t5 ("Refund payment") via "payment"
        //   - t2 ("Delete order fails gracefully") via "order && delete"
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchParens.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("payment || (order && delete)", () => GetVisibleScenarios().Count == 3);

        var visible = GetVisibleScenarios();
        Assert.Equal(3, visible.Count);
    }

    // ── Mixed operators ──

    [Fact]
    public void Mixed_text_tag_and_status_operators()
    {
        // "@api && $passed" should match only t1 (API + Passed); t2 is API but Failed
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchMixed.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("@api && $passed", () => GetVisibleScenarios().Count == 1);

        var visible = GetVisibleScenarios();
        Assert.Single(visible);
        Assert.Equal("Passed", visible[0].GetAttribute("data-status"));
        Assert.Contains("create order", visible[0].GetAttribute("data-search")!);
    }

    // ── Help icon / tooltip ──

    [Fact]
    public void Search_help_icon_is_present()
    {
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchHelpIcon.html"));
        WaitFor(By.CssSelector("details.feature"));

        var helpIcon = _driver.FindElement(By.CssSelector(".search-help-toggle"));
        Assert.NotNull(helpIcon);
        Assert.True(helpIcon.Displayed);
    }

    [Fact]
    public void Search_help_panel_toggles_on_click()
    {
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchHelpToggle.html"));
        WaitFor(By.CssSelector("details.feature"));

        var helpIcon = _driver.FindElement(By.CssSelector(".search-help-toggle"));
        var helpPanel = _driver.FindElement(By.CssSelector(".search-help-panel"));

        // Panel should be hidden initially
        Assert.Equal("none", helpPanel.GetCssValue("display"));

        // Click to show
        helpIcon.Click();
        Assert.NotEqual("none", helpPanel.GetCssValue("display"));

        // Click again to hide
        helpIcon.Click();
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(2));
        wait.Until(_ => helpPanel.GetCssValue("display") == "none");
        Assert.Equal("none", helpPanel.GetCssValue("display"));
    }

    [Fact]
    public void Search_help_panel_contains_syntax_reference()
    {
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchHelpContent.html"));
        WaitFor(By.CssSelector("details.feature"));

        var helpIcon = _driver.FindElement(By.CssSelector(".search-help-toggle"));
        helpIcon.Click();

        var helpPanel = _driver.FindElement(By.CssSelector(".search-help-panel"));
        var text = helpPanel.Text;

        // Should document all operators
        Assert.Contains("&&", text);
        Assert.Contains("||", text);
        Assert.Contains("!!", text);
        Assert.Contains("@tag", text);
        Assert.Contains("$status", text);
        Assert.Contains("parentheses", text.ToLowerInvariant());
    }

    // ── Feature name search ──

    [Fact]
    public void Search_by_feature_name_shows_all_scenarios_in_that_feature()
    {
        // "Order Feature" is a feature name — all its scenarios should be visible
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchFeatureName.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("order feature", () =>
        {
            var vis = GetVisibleScenarios();
            // Order Feature has 3 scenarios, Payment Feature has 2
            // Only Order Feature scenarios should match
            return vis.Count == 3;
        });

        var visible = GetVisibleScenarios();
        Assert.Equal(3, visible.Count);
    }

    [Fact]
    public void Search_by_feature_name_hides_other_features()
    {
        // "Payment Feature" should show only 2 scenarios from that feature
        _driver.Navigate().GoToUrl(GenerateReport("AdvSearchFeatureNameFilter.html"));
        WaitFor(By.CssSelector("details.feature"));

        SearchAndWait("payment feature", () =>
        {
            var vis = GetVisibleScenarios();
            return vis.Count == 2;
        });

        var visible = GetVisibleScenarios();
        Assert.Equal(2, visible.Count);

        // Order Feature should be hidden
        var features = _driver.FindElements(By.CssSelector("details.feature"))
            .Where(f => f.GetCssValue("display") != "none")
            .ToList();
        Assert.Single(features);
    }
}
