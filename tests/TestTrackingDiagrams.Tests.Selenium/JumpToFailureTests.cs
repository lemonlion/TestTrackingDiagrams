using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class JumpToFailureTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(JumpToFailureTests).Assembly.Location)!,
        "SeleniumOutput");

    public JumpToFailureTests()
    {
        _driver = ChromeDriverFactory.Create();
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-failure-" + Guid.NewGuid().ToString("N")[..8]);
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

    [Fact]
    public void Jump_to_failure_scrolls_scenario_title_into_view()
    {
        _driver.Navigate().GoToUrl(GenerateReport("JumpToFailureScroll.html"));

        // The test data has a failed scenario (t2). Find the "Next Failure" button.
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        var jumpBtn = wait.Until(d =>
        {
            try
            {
                var btn = d.FindElement(By.CssSelector("button.jump-to-failure"));
                return btn.Displayed ? btn : null;
            }
            catch (NoSuchElementException) { return null; }
        });
        Assert.NotNull(jumpBtn);

        // Click the jump button
        jumpBtn.Click();

        // Wait for smooth scroll to settle
        Thread.Sleep(1000);

        // The failed scenario's <summary> should be visible in the viewport
        var failedSummary = _driver.FindElement(By.CssSelector(
            "details.scenario[data-status='Failed'] > summary"));

        var isInViewport = (bool)((IJavaScriptExecutor)_driver).ExecuteScript("""
            var el = arguments[0];
            var rect = el.getBoundingClientRect();
            return rect.top >= 0 && rect.top < window.innerHeight;
            """, failedSummary)!;

        Assert.True(isInViewport, "Failed scenario summary should be visible in viewport after jump-to-failure");
    }

    [Fact]
    public void Jump_to_failure_counter_updates()
    {
        _driver.Navigate().GoToUrl(GenerateReport("JumpToFailureCounter.html"));

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        var jumpBtn = wait.Until(d =>
        {
            try
            {
                var btn = d.FindElement(By.CssSelector("button.jump-to-failure"));
                return btn.Displayed ? btn : null;
            }
            catch (NoSuchElementException) { return null; }
        });

        var counter = _driver.FindElement(By.Id("failure-counter"));
        Assert.Contains("0/", counter.Text);

        jumpBtn!.Click();
        Thread.Sleep(500);

        Assert.Contains("1/", counter.Text);
    }

    [Fact]
    public void Jump_to_failure_opens_feature_and_scenario()
    {
        _driver.Navigate().GoToUrl(GenerateReport("JumpToFailureOpens.html"));

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        var jumpBtn = wait.Until(d =>
        {
            try
            {
                var btn = d.FindElement(By.CssSelector("button.jump-to-failure"));
                return btn.Displayed ? btn : null;
            }
            catch (NoSuchElementException) { return null; }
        });

        jumpBtn!.Click();
        Thread.Sleep(1000);

        // The failed scenario should be open (has 'open' attribute)
        var failedScenario = _driver.FindElement(By.CssSelector(
            "details.scenario[data-status='Failed']"));
        Assert.Equal("true", failedScenario.GetAttribute("open"));

        // Its parent feature should also be open
        var parentFeature = failedScenario.FindElement(By.XPath("ancestor::details[contains(@class,'feature')]"));
        Assert.Equal("true", parentFeature.GetAttribute("open"));
    }
}
