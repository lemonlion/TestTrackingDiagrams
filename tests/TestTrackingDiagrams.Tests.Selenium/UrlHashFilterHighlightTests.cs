using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Selenium;

public class UrlHashFilterHighlightTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(UrlHashFilterHighlightTests).Assembly.Location)!,
        "SeleniumOutput");

    public UrlHashFilterHighlightTests()
    {
        _driver = ChromeDriverFactory.Create();
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-urlhash-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    private string GenerateAndServeReport(string fileName)
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Order Feature",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "t1", DisplayName = "Create order", IsHappyPath = true,
                        Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(2)
                    },
                    new Scenario
                    {
                        Id = "t2", DisplayName = "Delete order", IsHappyPath = false,
                        Result = ExecutionResult.Failed, Duration = TimeSpan.FromSeconds(5)
                    },
                    new Scenario
                    {
                        Id = "t3", DisplayName = "List orders", IsHappyPath = true,
                        Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1)
                    }
                ]
            }
        };

        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(_tempDir, fileName), "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);

        // Also copy to output for debugging
        File.Copy(path, Path.Combine(OutputDir, fileName), true);

        return new Uri(path).AbsoluteUri;
    }

    private IWebElement WaitFor(By by, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private string GetComputedBgColor(IWebElement element)
    {
        return (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return window.getComputedStyle(arguments[0]).backgroundColor;", element)!;
    }

    // The active button color is rgb(66, 133, 244)
    private static bool IsHighlighted(string bgColor) =>
        bgColor.Contains("66, 133, 244");

    [Fact]
    public void Status_filter_buttons_highlighted_after_url_hash_restore()
    {
        var url = GenerateAndServeReport("UrlHashStatusHighlight.html");
        _driver.Navigate().GoToUrl(url + "#status=Passed");

        // Wait for DOMContentLoaded to fire and parse_url_hash to run
        var passedBtn = WaitFor(By.CssSelector(".status-toggle[data-status='Passed']"));

        var bgColor = GetComputedBgColor(passedBtn);
        Assert.True(IsHighlighted(bgColor),
            $"Expected 'Passed' button to be highlighted (rgb(66, 133, 244)) but got: {bgColor}");
    }

    [Fact]
    public void Status_filter_hides_scenarios_and_highlights_button_on_url_restore()
    {
        var url = GenerateAndServeReport("UrlHashStatusHideHighlight.html");
        _driver.Navigate().GoToUrl(url + "#status=Failed");

        var failedBtn = WaitFor(By.CssSelector(".status-toggle[data-status='Failed']"));

        // Button should be highlighted
        Assert.True(IsHighlighted(GetComputedBgColor(failedBtn)),
            "Failed button should be highlighted after URL hash restore");

        // Passed scenarios should be hidden
        var passedScenarios = _driver.FindElements(By.CssSelector(".scenario[data-status='Passed']"));
        foreach (var scenario in passedScenarios)
        {
            Assert.Equal("none", scenario.GetCssValue("display"));
        }
    }

    [Fact]
    public void Happy_path_button_highlighted_after_url_hash_restore()
    {
        var url = GenerateAndServeReport("UrlHashHappyPathHighlight.html");
        _driver.Navigate().GoToUrl(url + "#hp=1");

        var hpBtn = WaitFor(By.CssSelector(".happy-path-toggle"));
        Assert.True(hpBtn.GetAttribute("class").Contains("happy-path-active"),
            "Happy path button should have happy-path-active class after URL hash restore");
        Assert.True(IsHighlighted(GetComputedBgColor(hpBtn)),
            $"Happy path button should be highlighted but got: {GetComputedBgColor(hpBtn)}");
    }

    [Fact]
    public void Search_text_restored_after_url_hash_restore()
    {
        var url = GenerateAndServeReport("UrlHashSearchRestore.html");
        _driver.Navigate().GoToUrl(url + "#q=" + Uri.EscapeDataString("Create order"));

        var searchBar = WaitFor(By.Id("searchbar"));
        Assert.Equal("Create order", searchBar.GetAttribute("value"));
    }

    [Fact]
    public void User_report_status_filter_highlighted_after_url_hash_restore()
    {
        var reportPath = @"C:\dev\Stratus.Migration.MigrationService\tests\Stratus.Migration.MigrationService.Tests.Component\bin\Debug\net8.0\Reports\TestRunReport - Copy.html";
        if (!File.Exists(reportPath))
            return; // Skip if the user's report doesn't exist on this machine

        var url = new Uri(reportPath).AbsoluteUri;
        _driver.Navigate().GoToUrl(url + "#status=Passed");

        // Wait for DOMContentLoaded + parse_url_hash to complete
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var passedBtn = wait.Until(d => d.FindElement(By.CssSelector(".status-toggle[data-status='Passed']")));

        var hasActiveClass = passedBtn.GetAttribute("class").Contains("status-active");
        var bgColor = GetComputedBgColor(passedBtn);

        Assert.True(hasActiveClass,
            $"Expected 'Passed' button to have 'status-active' class but classes were: {passedBtn.GetAttribute("class")}");
        Assert.True(IsHighlighted(bgColor),
            $"Expected 'Passed' button to be highlighted (rgb(66, 133, 244)) but got: {bgColor}");
    }

    [Fact]
    public void Status_filter_highlighted_after_click_then_refresh()
    {
        var url = GenerateAndServeReport("UrlHashClickRefresh.html");
        _driver.Navigate().GoToUrl(url);

        // Click the "Passed" status button
        var passedBtn = WaitFor(By.CssSelector(".status-toggle[data-status='Passed']"));
        passedBtn.Click();

        // Verify it's highlighted after click
        Assert.True(IsHighlighted(GetComputedBgColor(passedBtn)),
            "Button should be highlighted after click");

        // Capture the current URL with hash
        var urlWithHash = _driver.Url;
        Assert.Contains("#", urlWithHash);

        // Refresh the page
        _driver.Navigate().Refresh();

        // Wait for DOM to load
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var passedBtnAfterRefresh = wait.Until(d => d.FindElement(By.CssSelector(".status-toggle[data-status='Passed']")));

        var hasActiveClass = passedBtnAfterRefresh.GetAttribute("class")!.Contains("status-active");
        var bgColorAfterRefresh = GetComputedBgColor(passedBtnAfterRefresh);

        Assert.True(hasActiveClass,
            $"Expected 'Passed' button to have 'status-active' class after refresh but classes were: {passedBtnAfterRefresh.GetAttribute("class")}");
        Assert.True(IsHighlighted(bgColorAfterRefresh),
            $"Expected 'Passed' button to be highlighted after refresh but got: {bgColorAfterRefresh}");
    }

    [Fact]
    public void Happy_path_highlighted_after_click_then_refresh()
    {
        var url = GenerateAndServeReport("UrlHashHPClickRefresh.html");
        _driver.Navigate().GoToUrl(url);

        var hpBtn = WaitFor(By.CssSelector(".happy-path-toggle"));
        hpBtn.Click();

        Assert.True(IsHighlighted(GetComputedBgColor(hpBtn)),
            "HP button should be highlighted after click");

        _driver.Navigate().Refresh();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var hpBtnAfterRefresh = wait.Until(d => d.FindElement(By.CssSelector(".happy-path-toggle")));

        Assert.True(hpBtnAfterRefresh.GetAttribute("class")!.Contains("happy-path-active"),
            $"Expected HP button to have 'happy-path-active' class after refresh but classes were: {hpBtnAfterRefresh.GetAttribute("class")}");
        Assert.True(IsHighlighted(GetComputedBgColor(hpBtnAfterRefresh)),
            $"Expected HP button highlighted after refresh but got: {GetComputedBgColor(hpBtnAfterRefresh)}");
    }

    [Fact]
    public void Multiple_status_filters_highlighted_after_click_then_refresh()
    {
        var url = GenerateAndServeReport("UrlHashMultiStatusRefresh.html");
        _driver.Navigate().GoToUrl(url);

        // Click "Passed" and "Failed" buttons
        WaitFor(By.CssSelector(".status-toggle[data-status='Passed']")).Click();
        WaitFor(By.CssSelector(".status-toggle[data-status='Failed']")).Click();

        _driver.Navigate().Refresh();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var passedBtn = wait.Until(d => d.FindElement(By.CssSelector(".status-toggle[data-status='Passed']")));
        var failedBtn = _driver.FindElement(By.CssSelector(".status-toggle[data-status='Failed']"));
        var skippedBtn = _driver.FindElement(By.CssSelector(".status-toggle[data-status='Skipped']"));

        Assert.True(passedBtn.GetAttribute("class")!.Contains("status-active"),
            "Passed should be active after refresh");
        Assert.True(failedBtn.GetAttribute("class")!.Contains("status-active"),
            "Failed should be active after refresh");
        Assert.False(skippedBtn.GetAttribute("class")!.Contains("status-active"),
            "Skipped should NOT be active after refresh");
    }

    [Fact]
    public void Status_and_happy_path_both_highlighted_after_click_then_refresh()
    {
        var url = GenerateAndServeReport("UrlHashComboRefresh.html");
        _driver.Navigate().GoToUrl(url);

        WaitFor(By.CssSelector(".status-toggle[data-status='Failed']")).Click();
        WaitFor(By.CssSelector(".happy-path-toggle")).Click();

        _driver.Navigate().Refresh();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var failedBtn = wait.Until(d => d.FindElement(By.CssSelector(".status-toggle[data-status='Failed']")));
        var hpBtn = _driver.FindElement(By.CssSelector(".happy-path-toggle"));

        Assert.True(failedBtn.GetAttribute("class")!.Contains("status-active"),
            $"Failed should be active after refresh but classes: {failedBtn.GetAttribute("class")}");
        Assert.True(hpBtn.GetAttribute("class")!.Contains("happy-path-active"),
            $"HP should be active after refresh but classes: {hpBtn.GetAttribute("class")}");
    }

    [Fact]
    public void Percentile_button_highlighted_after_click_then_refresh()
    {
        var url = GenerateAndServeReport("UrlHashP99Refresh.html");
        _driver.Navigate().GoToUrl(url);

        // Click the P99 percentile button
        var p99Btn = WaitFor(By.XPath("//button[contains(@class,'percentile-btn') and contains(text(),'P99')]"));
        p99Btn.Click();

        // Verify it's highlighted after click
        Assert.True(p99Btn.GetAttribute("class")!.Contains("percentile-active"),
            "P99 button should have percentile-active class after click");

        // Capture URL hash
        var urlWithHash = _driver.Url;
        Assert.Contains("pctl=", urlWithHash);

        // Refresh the page
        _driver.Navigate().Refresh();

        // Wait for DOM to load and check highlight
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var p99BtnAfterRefresh = wait.Until(d =>
            d.FindElement(By.XPath("//button[contains(@class,'percentile-btn') and contains(text(),'P99')]")));

        Assert.True(p99BtnAfterRefresh.GetAttribute("class")!.Contains("percentile-active"),
            $"P99 button should have percentile-active class after refresh but classes: {p99BtnAfterRefresh.GetAttribute("class")}");
        Assert.True(IsHighlighted(GetComputedBgColor(p99BtnAfterRefresh)),
            $"P99 button should be highlighted after refresh but got: {GetComputedBgColor(p99BtnAfterRefresh)}");
    }
}
