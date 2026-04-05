using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class WholeTestFlowTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(WholeTestFlowTests).Assembly.Location)!,
        "SeleniumOutput");

    public WholeTestFlowTests()
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
        File.WriteAllText(Path.Combine(OutputDir, $"{testName}.html"), html);
        return new Uri(path).AbsoluteUri;
    }

    private IWebElement WaitFor(By by, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private string GetComputedStyle(IWebElement element, string property)
    {
        return (string)((IJavaScriptExecutor)_driver).ExecuteScript(
            "return window.getComputedStyle(arguments[0]).getPropertyValue(arguments[1]);",
            element, property);
    }

    // ── Details block ──

    [Fact]
    public void Whole_test_flow_renders_collapsed_details_block()
    {
        var html = TestPageGenerator.GenerateWholeTestFlowPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        var details = WaitFor(By.CssSelector("details.whole-test-flow"));
        Assert.True(details.Displayed);

        // Should be collapsed by default (no 'open' attribute)
        var isOpen = details.GetAttribute("open");
        Assert.Null(isOpen);

        var summary = details.FindElement(By.TagName("summary"));
        Assert.Contains("Whole Test Flow", summary.Text);
        Assert.Contains("span", summary.Text);
    }

    [Fact]
    public void Whole_test_flow_expands_on_click()
    {
        var html = TestPageGenerator.GenerateWholeTestFlowPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        var summary = WaitFor(By.CssSelector("details.whole-test-flow > summary"));
        summary.Click();

        var details = _driver.FindElement(By.CssSelector("details.whole-test-flow"));
        Assert.Equal("true", details.GetAttribute("open"));
    }

    [Fact]
    public void Whole_test_flow_Both_shows_toggle_buttons()
    {
        var html = TestPageGenerator.GenerateWholeTestFlowPage(WholeTestFlowVisualization.Both);
        _driver.Navigate().GoToUrl(ServePage(html));

        // Expand the details
        WaitFor(By.CssSelector("details.whole-test-flow > summary")).Click();

        var toggleBtns = _driver.FindElements(By.CssSelector(".whole-test-flow .iflow-toggle-btn"));
        Assert.Equal(2, toggleBtns.Count);
        Assert.Equal("Activity", toggleBtns[0].Text);
        Assert.Equal("Flame Chart", toggleBtns[1].Text);
    }

    [Fact]
    public void Whole_test_flow_toggle_switches_views()
    {
        var html = TestPageGenerator.GenerateWholeTestFlowPage(WholeTestFlowVisualization.Both);
        _driver.Navigate().GoToUrl(ServePage(html));

        WaitFor(By.CssSelector("details.whole-test-flow > summary")).Click();

        var toggleBtns = _driver.FindElements(By.CssSelector(".whole-test-flow .iflow-toggle-btn"));

        // Activity view visible by default
        var mainView = _driver.FindElement(By.CssSelector(".whole-test-flow .iflow-view-main"));
        var flameView = _driver.FindElement(By.CssSelector(".whole-test-flow .iflow-view-flame"));
        Assert.True(mainView.Displayed);
        Assert.False(flameView.Displayed);

        // Click Flame Chart toggle
        toggleBtns[1].Click();
        Assert.False(mainView.Displayed);
        Assert.True(flameView.Displayed);

        // Click Activity toggle back
        toggleBtns[0].Click();
        Assert.True(mainView.Displayed);
        Assert.False(flameView.Displayed);
    }

    [Fact]
    public void Whole_test_flow_FlameChart_only_shows_flame_bars()
    {
        var html = TestPageGenerator.GenerateWholeTestFlowPage(WholeTestFlowVisualization.FlameChart);
        _driver.Navigate().GoToUrl(ServePage(html));

        WaitFor(By.CssSelector("details.whole-test-flow > summary")).Click();

        var flameBars = _driver.FindElements(By.CssSelector(".whole-test-flow .iflow-flame-bar"));
        Assert.True(flameBars.Count >= 2, $"Expected at least 2 flame bars, got {flameBars.Count}");

        // No toggle buttons for single mode
        var toggleBtns = _driver.FindElements(By.CssSelector(".whole-test-flow .iflow-toggle-btn"));
        Assert.Empty(toggleBtns);
    }

    [Fact]
    public void Whole_test_flow_flame_has_boundary_markers()
    {
        var html = TestPageGenerator.GenerateWholeTestFlowPage(WholeTestFlowVisualization.FlameChart);
        _driver.Navigate().GoToUrl(ServePage(html));

        WaitFor(By.CssSelector("details.whole-test-flow > summary")).Click();

        var markers = _driver.FindElements(By.CssSelector(".whole-test-flow .iflow-boundary-marker"));
        Assert.True(markers.Count >= 1, "Expected at least 1 boundary marker");
    }
}
