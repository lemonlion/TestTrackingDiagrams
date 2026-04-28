using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TestTrackingDiagrams.Tests.Selenium;

public class ComponentFlowPopupTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(ComponentFlowPopupTests).Assembly.Location)!,
        "SeleniumOutput");

    public ComponentFlowPopupTests()
    {
        _driver = ChromeDriverFactory.Create(1280, 900);
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

    // ── Relationship list ──

    [Fact]
    public void Relationship_list_is_rendered()
    {
        var html = TestPageGenerator.GenerateComponentFlowPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        var list = WaitFor(By.CssSelector(".iflow-rel-list"));
        Assert.True(list.Displayed);

        var items = list.FindElements(By.TagName("li"));
        Assert.Single(items);
        Assert.Contains("API", items[0].Text);
        Assert.Contains("DB", items[0].Text);
    }

    // ── Relationship popup ──

    [Fact]
    public void Clicking_relationship_opens_popup_with_flow_diagram()
    {
        var html = TestPageGenerator.GenerateComponentFlowPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("rel-api-db")).Click();

        var popup = WaitFor(By.CssSelector(".iflow-popup"));
        Assert.True(popup.Displayed);

        var heading = popup.FindElement(By.TagName("h3"));
        Assert.Contains("API", heading.Text);
        Assert.Contains("DB", heading.Text);

        // Wait for activity diagram to render
        var svg = WaitForActivityDiagramSvg(popup);
        Assert.True(svg.Displayed);
    }

    [Fact]
    public void Relationship_popup_contains_summary_table()
    {
        var html = TestPageGenerator.GenerateComponentFlowPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("rel-api-db")).Click();
        var popup = WaitFor(By.CssSelector(".iflow-popup"));

        var table = popup.FindElement(By.CssSelector(".iflow-rel-summary-table"));
        Assert.True(table.Displayed);

        var rows = table.FindElements(By.TagName("tr"));
        Assert.True(rows.Count >= 3, $"Expected header + 2 data rows, got {rows.Count}");

        var tableText = table.Text;
        Assert.Contains("Order Creation Test", tableText);
        Assert.Contains("Payment Flow Test", tableText);
    }

    [Fact]
    public void Relationship_popup_close_button_works()
    {
        var html = TestPageGenerator.GenerateComponentFlowPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("rel-api-db")).Click();
        WaitFor(By.CssSelector(".iflow-popup"));

        _driver.FindElement(By.CssSelector(".iflow-popup-close")).Click();

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        Assert.True(wait.Until(d =>
        {
            try { return !d.FindElement(By.CssSelector(".iflow-overlay")).Displayed; }
            catch (NoSuchElementException) { return true; }
        }));
    }

    [Fact]
    public void Activity_diagram_popup_does_not_show_loading_text_after_render()
    {
        var html = TestPageGenerator.GenerateComponentFlowPage();
        _driver.Navigate().GoToUrl(ServePage(html));

        _driver.FindElement(By.Id("rel-api-db")).Click();

        var popup = WaitFor(By.CssSelector(".iflow-popup"));
        WaitForActivityDiagramSvg(popup);

        // After the diagram SVG has rendered, data-queued and data-rendered must be set
        // so the CSS ::before pseudo-element with loading text is suppressed.
        var attrs = (string)((IJavaScriptExecutor)_driver).ExecuteScript("""
            var el = document.querySelector('.iflow-popup .plantuml-browser');
            if (!el) return 'element-not-found';
            return (el.dataset.queued || 'missing') + '|' + (el.dataset.rendered || 'missing');
            """)!;

        Assert.Equal("1|1", attrs);
    }
}
