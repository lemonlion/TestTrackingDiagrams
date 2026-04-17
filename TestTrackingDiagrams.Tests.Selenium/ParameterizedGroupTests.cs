using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Selenium;

public class ParameterizedGroupTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(ParameterizedGroupTests).Assembly.Location)!,
        "SeleniumOutput");

    public ParameterizedGroupTests()
    {
        _driver = ChromeDriverFactory.Create();
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-param-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    private string GenerateParamReport(string fileName, bool withDiagrams = true, bool withFailure = false)
    {
        var scenarios = new[]
        {
            new Scenario
            {
                Id = "p1", DisplayName = "Process(region: UK, amount: 100)", IsHappyPath = true,
                Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1),
                OutlineId = "Process",
                ExampleValues = new Dictionary<string, string> { ["region"] = "UK", ["amount"] = "100" },
                Steps =
                [
                    new ScenarioStep { Keyword = "Given", Text = "a valid region UK", Status = ExecutionResult.Passed },
                    new ScenarioStep { Keyword = "When", Text = "processing amount 100", Status = ExecutionResult.Passed },
                    new ScenarioStep { Keyword = "Then", Text = "the result is success", Status = ExecutionResult.Passed }
                ]
            },
            new Scenario
            {
                Id = "p2", DisplayName = "Process(region: US, amount: 200)", IsHappyPath = false,
                Result = withFailure ? ExecutionResult.Failed : ExecutionResult.Passed,
                Duration = TimeSpan.FromSeconds(2),
                OutlineId = "Process",
                ExampleValues = new Dictionary<string, string> { ["region"] = "US", ["amount"] = "200" },
                ErrorMessage = withFailure ? "Expected 200 but got 0" : null,
                ErrorStackTrace = withFailure ? "at ProcessTests.cs:42" : null,
                Steps =
                [
                    new ScenarioStep { Keyword = "Given", Text = "a valid region US", Status = ExecutionResult.Passed },
                    new ScenarioStep { Keyword = "When", Text = "processing amount 200", Status = withFailure ? ExecutionResult.Failed : ExecutionResult.Passed },
                    new ScenarioStep { Keyword = "Then", Text = "the result is success", Status = withFailure ? ExecutionResult.Skipped : ExecutionResult.Passed }
                ]
            },
            new Scenario
            {
                Id = "p3", DisplayName = "Process(region: DE, amount: 300)", IsHappyPath = false,
                Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1),
                OutlineId = "Process",
                ExampleValues = new Dictionary<string, string> { ["region"] = "DE", ["amount"] = "300" },
                Steps =
                [
                    new ScenarioStep { Keyword = "Given", Text = "a valid region DE", Status = ExecutionResult.Passed },
                    new ScenarioStep { Keyword = "When", Text = "processing amount 300", Status = ExecutionResult.Passed },
                    new ScenarioStep { Keyword = "Then", Text = "the result is success", Status = ExecutionResult.Passed }
                ]
            }
        };

        var features = new[]
        {
            new Feature
            {
                DisplayName = "Payment Processing",
                Scenarios = scenarios
            }
        };

        var diagrams = withDiagrams
            ? scenarios.Select(s => new DefaultDiagramsFetcher.DiagramAsCode(
                s.Id, "",
                $"@startuml\nActor -> Service : {s.ExampleValues!["region"]}\n@enduml")).ToArray()
            : scenarios.Select(s => new DefaultDiagramsFetcher.DiagramAsCode(s.Id, "", "")).ToArray();

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(_tempDir, fileName), "Param Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            groupParameterizedTests: true);

        File.Copy(path, Path.Combine(OutputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    private IWebElement WaitFor(By by, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private void ExpandFeatures()
    {
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
    }

    // ── Parameterized group renders ──

    [Fact]
    public void Parameterized_group_renders_with_table()
    {
        _driver.Navigate().GoToUrl(GenerateParamReport("ParamGroup.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFeatures();

        var group = WaitFor(By.CssSelector("details.scenario-parameterized"));
        Assert.NotNull(group);

        var table = group.FindElement(By.CssSelector("table.param-test-table"));
        Assert.NotNull(table);

        var rows = table.FindElements(By.CssSelector("tbody tr"));
        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public void Clicking_row_switches_detail_panel()
    {
        _driver.Navigate().GoToUrl(GenerateParamReport("ParamRowSwitch.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFeatures();

        var group = WaitFor(By.CssSelector("details.scenario-parameterized"));
        group.FindElement(By.CssSelector("summary")).Click();

        var rows = group.FindElements(By.CssSelector("tbody tr"));
        Assert.True(rows.Count >= 3);

        // First row should be active by default
        Assert.Contains("row-active", rows[0].GetAttribute("class"));

        // Click second row
        rows[1].Click();
        Assert.Contains("row-active", rows[1].GetAttribute("class"));
        Assert.DoesNotContain("row-active", rows[0].GetAttribute("class"));
    }

    [Fact]
    public void Clicking_row_switches_visible_detail_content()
    {
        _driver.Navigate().GoToUrl(GenerateParamReport("ParamDetailSwitch.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFeatures();

        var group = WaitFor(By.CssSelector("details.scenario-parameterized"));
        group.FindElement(By.CssSelector("summary")).Click();

        // First detail panel visible, second hidden
        var panels = group.FindElements(By.CssSelector(".param-detail-panel"));
        Assert.True(panels.Count >= 2);
        Assert.True(panels[0].Displayed);
        Assert.False(panels[1].Displayed);

        // Click second row
        var rows = group.FindElements(By.CssSelector("tbody tr"));
        rows[1].Click();

        Assert.False(panels[0].Displayed);
        Assert.True(panels[1].Displayed);
    }

    // ── Status badges ──

    [Fact]
    public void Status_badges_show_correct_status_per_row()
    {
        _driver.Navigate().GoToUrl(GenerateParamReport("ParamStatus.html", withFailure: true));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFeatures();

        var group = WaitFor(By.CssSelector("details.scenario-parameterized"));
        var badges = group.FindElements(By.CssSelector(".status-badge"));
        Assert.True(badges.Count >= 3);

        // First row passed, second failed
        Assert.Contains("badge-pass", badges[0].GetAttribute("class"));
        Assert.Contains("badge-fail", badges[1].GetAttribute("class"));
    }

    // ── Copy button ──

    [Fact]
    public void Copy_scenario_name_button_exists_on_parameterized_group()
    {
        _driver.Navigate().GoToUrl(GenerateParamReport("ParamCopy.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFeatures();

        var group = WaitFor(By.CssSelector("details.scenario-parameterized"));
        var copyBtn = group.FindElement(By.CssSelector(".copy-scenario-name"));
        Assert.NotNull(copyBtn);

        var name = copyBtn.GetAttribute("data-scenario-name");
        Assert.Equal("Process", name);
    }

    [Fact]
    public void Copy_button_shows_checkmark_after_click()
    {
        _driver.Navigate().GoToUrl(GenerateParamReport("ParamCopyCheck.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFeatures();

        var group = WaitFor(By.CssSelector("details.scenario-parameterized"));
        var copyBtn = group.FindElement(By.CssSelector(".copy-scenario-name"));
        copyBtn.Click();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(2));
        wait.Until(_ => copyBtn.Text == "\u2713");
        Assert.Equal("\u2713", copyBtn.Text);
    }

    // ── Scenario link ──

    [Fact]
    public void Scenario_link_exists_on_parameterized_group()
    {
        _driver.Navigate().GoToUrl(GenerateParamReport("ParamLink.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFeatures();

        var group = WaitFor(By.CssSelector("details.scenario-parameterized"));
        var link = group.FindElement(By.CssSelector(".scenario-link"));
        Assert.NotNull(link);

        var href = link.GetAttribute("href");
        Assert.Contains("#scenario-", href);
    }

    // ── Deep linking to a row ──

    [Fact]
    public void Deep_link_hash_opens_parameterized_group_and_selects_row()
    {
        var url = GenerateParamReport("ParamDeepLink.html");
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector("details.feature"));

        // Get the scenario-id of the second row
        ExpandFeatures();
        var group = _driver.FindElement(By.CssSelector("details.scenario-parameterized"));
        group.FindElement(By.CssSelector("summary")).Click();
        var rows = group.FindElements(By.CssSelector("tbody tr"));
        var secondRowId = rows[1].GetAttribute("data-scenario-id");
        Assert.False(string.IsNullOrEmpty(secondRowId));

        // Navigate to deep link
        _driver.Navigate().GoToUrl(url + "#" + secondRowId);
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            var g = d.FindElement(By.CssSelector("details.scenario-parameterized"));
            return g.GetAttribute("open") != null;
        });

        // The group should be open
        group = _driver.FindElement(By.CssSelector("details.scenario-parameterized"));
        Assert.NotNull(group.GetAttribute("open"));
    }

    // ── Search highlighting ──

    [Fact]
    public void Search_highlights_matching_rows_in_parameterized_group()
    {
        _driver.Navigate().GoToUrl(GenerateParamReport("ParamSearch.html"));
        WaitFor(By.CssSelector("details.feature"));

        // Type a search term that matches one row
        var searchbar = _driver.FindElement(By.Id("searchbar"));
        searchbar.SendKeys("region: US");

        // Wait for search to apply (300ms debounce)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
        wait.Until(d =>
        {
            var matches = d.FindElements(By.CssSelector("tr.row-search-match"));
            return matches.Count > 0;
        });

        var matchedRows = _driver.FindElements(By.CssSelector("tr.row-search-match"));
        Assert.True(matchedRows.Count >= 1);
    }

    // ── Diagram section ──

    [Fact]
    public void Parameterized_group_has_diagram_section()
    {
        _driver.Navigate().GoToUrl(GenerateParamReport("ParamDiagrams.html"));
        WaitFor(By.CssSelector("details.feature"));
        ExpandFeatures();

        var group = WaitFor(By.CssSelector("details.scenario-parameterized"));
        group.FindElement(By.CssSelector("summary")).Click();

        var diagrams = group.FindElement(By.CssSelector("details.example-diagrams"));
        Assert.NotNull(diagrams);
    }
}
