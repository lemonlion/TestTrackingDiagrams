using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class LoadingMessageTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(LoadingMessageTests).Assembly.Location)!,
        "SeleniumOutput");

    public LoadingMessageTests()
    {
        _driver = ChromeDriverFactory.Create();
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-loading-" + Guid.NewGuid().ToString("N")[..8]);
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

    private void ExpandFirstScenarioWithDiagram()
    {
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Features')]")).Click();
        _driver.FindElement(By.XPath(
            "//button[contains(@class,'collapse-expand-all') and contains(text(),'Expand All Scenarios')]")).Click();
    }

    [Fact]
    public void Body_has_plantuml_ready_class_after_page_load()
    {
        _driver.Navigate().GoToUrl(GenerateReport("BodyPlantumlReady.html"));

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d =>
        {
            var body = d.FindElement(By.TagName("body"));
            var classes = body.GetAttribute("class") ?? "";
            return classes.Contains("plantuml-ready");
        });

        var bodyClasses = _driver.FindElement(By.TagName("body")).GetAttribute("class")!;
        Assert.Contains("plantuml-ready", bodyClasses);
    }

    [Fact]
    public void Unrendered_diagram_shows_rendering_message_not_waiting()
    {
        _driver.Navigate().GoToUrl(GenerateReport("LoadingMsgRendering.html"));

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d => d.FindElement(By.TagName("body")).GetAttribute("class")!.Contains("plantuml-ready"));

        ExpandFirstScenarioWithDiagram();

        // Find a diagram container that hasn't rendered yet
        wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        var message = wait.Until(d =>
        {
            // Get ::before content via JS
            var content = (string?)((IJavaScriptExecutor)d).ExecuteScript("""
                var diagrams = document.querySelectorAll('.plantuml-browser:not([data-rendered])');
                for (var i = 0; i < diagrams.length; i++) {
                    var before = window.getComputedStyle(diagrams[i], '::before').getPropertyValue('content');
                    if (before && before !== 'none' && before !== 'normal') return before;
                }
                return null;
                """);
            return content;
        });

        Assert.NotNull(message);
        Assert.DoesNotContain("Waiting for page load", message);
        Assert.Contains("Rendering diagram", message);
    }
}
