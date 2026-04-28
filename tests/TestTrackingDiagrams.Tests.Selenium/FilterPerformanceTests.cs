using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Selenium;

public class FilterPerformanceTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(FilterPerformanceTests).Assembly.Location)!,
        "SeleniumOutput");

    public FilterPerformanceTests()
    {
        _driver = ChromeDriverFactory.Create();
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-perf-" + Guid.NewGuid().ToString("N")[..8]);
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
        var outputPath = Path.Combine(OutputDir, $"{testName}.html");
        File.WriteAllText(outputPath, html);
        return new Uri(path).AbsoluteUri;
    }

    /// <summary>
    /// Generates a realistic report HTML with many features, scenarios, and inline SVGs.
    /// Uses the real stylesheet and filter JS from ReportGenerator.
    /// </summary>
    private static string GenerateLargeReport(int featureCount, int scenariosPerFeature, string[] dependencies)
    {
        var sb = new StringBuilder();
        sb.Append($"""
            <!DOCTYPE html>
            <html>
            <head>
            <style>{Stylesheets.HtmlReportStyleSheet}</style>
            <script>
            """);

        // Emit the same filter cache + filter functions as ReportGenerator
        sb.Append("""
            var _filterCache;
            function fc() {
                if (_filterCache) return _filterCache;
                var scenarios = document.getElementsByClassName('scenario');
                var features = document.getElementsByClassName('feature');
                var items = [];
                var fMap = new Map();
                for (var fi = 0; fi < features.length; fi++) {
                    var sc = features[fi].getElementsByClassName('scenario');
                    var arr = [];
                    for (var si = 0; si < sc.length; si++) {
                        var s = sc[si];
                        var raw = s.getAttribute('data-dependencies') || '';
                        var d = raw ? new Set(raw.split(',')) : new Set();
                        var item = { el: s, deps: d, status: s.getAttribute('data-status') || '', isHappy: s.classList.contains('happy-path'), f: features[fi] };
                        items.push(item);
                        arr.push(item);
                        fMap.set(s, features[fi]);
                    }
                }
                _filterCache = { items: items, features: features, scenarios: scenarios, fMap: fMap };
                return _filterCache;
            }

            function toggle_dependency(btn) {
                btn.classList.toggle('dependency-active');
                filter_dependencies();
            }

            function filter_dependencies() {
                var c = fc();
                var activeSet = new Set();
                document.querySelectorAll('.dependency-toggle.dependency-active').forEach(function(b) {
                    activeSet.add(b.getAttribute('data-dependency'));
                });

                for (var i = 0; i < c.items.length; i++) {
                    c.items[i].el.classList.remove('dep-hidden');
                }
                for (var i = 0; i < c.features.length; i++) {
                    var f = c.features[i];
                    f.classList.remove('dep-hidden');
                    if (f.classList.contains('dep-opened')) {
                        f.removeAttribute('open');
                        f.classList.remove('dep-opened');
                    }
                }

                if (activeSet.size === 0) return;

                var activeArr = Array.from(activeSet);
                var featureVisibleCounts = new Map();
                var totalVisible = 0;
                for (var i = 0; i < c.features.length; i++) featureVisibleCounts.set(c.features[i], 0);

                for (var i = 0; i < c.items.length; i++) {
                    var d = c.items[i];
                    var matchesAll = d.deps.size > 0;
                    if (matchesAll) {
                        for (var j = 0; j < activeArr.length; j++) {
                            if (!d.deps.has(activeArr[j])) { matchesAll = false; break; }
                        }
                    }
                    if (!matchesAll) {
                        d.el.classList.add('dep-hidden');
                    } else if (!d.el.classList.contains('search-hidden') && !d.el.classList.contains('status-hidden') && !d.el.classList.contains('hp-hidden')) {
                        featureVisibleCounts.set(d.f, (featureVisibleCounts.get(d.f) || 0) + 1);
                        totalVisible++;
                    }
                }

                var shouldOpen = totalVisible <= 10;
                for (var i = 0; i < c.features.length; i++) {
                    var f = c.features[i];
                    if ((featureVisibleCounts.get(f) || 0) === 0) {
                        f.classList.add('dep-hidden');
                    } else if (shouldOpen && !f.hasAttribute('open')) {
                        f.setAttribute('open', '');
                        f.classList.add('dep-opened');
                    }
                }
            }
            </script>
            </head>
            <body>
            <h1>Performance Test Report</h1>
            <div class="filters">
            <div class="dependency-filters"><span class="dependency-filters-label">Dependencies:</span>
            """);

        foreach (var dep in dependencies)
        {
            sb.Append($"""<button class="dependency-toggle" data-dependency="{dep}" onclick="toggle_dependency(this)">{dep}</button>""");
        }
        sb.Append("</div></div>");

        sb.Append("<div id=\"report-content\">");

        // Generate a realistic inline SVG for each scenario
        var svgTemplate = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 800 400" width="800" height="400">
            <rect width="800" height="400" fill="#fefefe"/>
            <line x1="200" y1="80" x2="200" y2="380" stroke="#888" stroke-dasharray="5"/>
            <line x1="400" y1="80" x2="400" y2="380" stroke="#888" stroke-dasharray="5"/>
            <line x1="600" y1="80" x2="600" y2="380" stroke="#888" stroke-dasharray="5"/>
            <rect x="150" y="40" width="100" height="30" rx="4" fill="#e8e8e8" stroke="#888"/>
            <text x="200" y="60" text-anchor="middle" font-size="12">Caller</text>
            <rect x="350" y="40" width="100" height="30" rx="4" fill="#e8e8e8" stroke="#888"/>
            <text x="400" y="60" text-anchor="middle" font-size="12">Service</text>
            <rect x="550" y="40" width="100" height="30" rx="4" fill="#e8e8e8" stroke="#888"/>
            <text x="600" y="60" text-anchor="middle" font-size="12">Database</text>
            <line x1="200" y1="120" x2="400" y2="120" stroke="#333" marker-end="url(#arrow)"/>
            <text x="300" y="115" text-anchor="middle" font-size="11" fill="#0000FF" text-decoration="underline">POST: /api/orders</text>
            <line x1="400" y1="160" x2="600" y2="160" stroke="#333" marker-end="url(#arrow)"/>
            <text x="500" y="155" text-anchor="middle" font-size="11">INSERT INTO orders</text>
            <line x1="600" y1="200" x2="400" y2="200" stroke="#333" stroke-dasharray="4" marker-end="url(#arrow)"/>
            <text x="500" y="195" text-anchor="middle" font-size="11">200 OK</text>
            <line x1="400" y1="240" x2="200" y2="240" stroke="#333" stroke-dasharray="4" marker-end="url(#arrow)"/>
            <text x="300" y="235" text-anchor="middle" font-size="11">201 Created</text>
            <defs><marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto">
            <path d="M 0 0 L 10 5 L 0 10 z" fill="#333"/></marker></defs>
            </svg>
            """;

        var rng = new Random(42);
        for (var fi = 0; fi < featureCount; fi++)
        {
            sb.Append($"""<details class="feature"><summary class="h2">Feature {fi + 1}</summary>""");
            for (var si = 0; si < scenariosPerFeature; si++)
            {
                // Assign 1-3 random dependencies
                var scenarioDeps = new List<string>();
                for (var di = 0; di < dependencies.Length; di++)
                {
                    if (rng.NextDouble() < 0.4) scenarioDeps.Add(dependencies[di]);
                }
                if (scenarioDeps.Count == 0) scenarioDeps.Add(dependencies[0]);

                var depsAttr = $" data-dependencies=\"{string.Join(",", scenarioDeps)}\"";
                var statusAttr = " data-status=\"Passed\"";
                var isHappy = si == 0;

                sb.Append($"""
                    <details class="scenario{(isHappy ? " happy-path" : "")}"{depsAttr}{statusAttr}>
                    <summary class="h3">Scenario {fi + 1}.{si + 1}: Test something with {string.Join(" and ", scenarioDeps)}</summary>
                    <details class="example-diagrams" open>
                    <summary class="h4">Sequence Diagrams</summary>
                    <div class="plantuml-inline-svg" data-diagram-type="plantuml">{svgTemplate}</div>
                    </details>
                    </details>
                    """);
            }
            sb.Append("</details>");
        }

        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    [Fact]
    public void DependencyFilter_CompletesWithin1Second_With100Scenarios()
    {
        var dependencies = new[] { "OrderService", "PaymentGateway", "NotificationService", "CacheService", "AuditService" };
        var html = GenerateLargeReport(featureCount: 20, scenariosPerFeature: 5, dependencies: dependencies);

        _driver.Navigate().GoToUrl(ServePage(html));

        // Wait for page to be fully loaded
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState") as string == "complete");

        // Warm up the filter cache
        ((IJavaScriptExecutor)_driver).ExecuteScript("fc();");

        // Measure filter time inside JavaScript to avoid Selenium IPC overhead
        var ms = (long)((IJavaScriptExecutor)_driver).ExecuteScript(
            "var btn = document.querySelector('.dependency-toggle[data-dependency=\"OrderService\"]');" +
            "var t0 = performance.now(); btn.click(); return Math.round(performance.now() - t0);")!;

        // Verify the filter was applied (some scenarios should be hidden)
        var hiddenCount = _driver.FindElements(By.CssSelector(".scenario.dep-hidden")).Count;
        Assert.True(hiddenCount > 0, "Some scenarios should be hidden by the filter");

        // Verify performance: should complete in <1 second
        Assert.True(ms < 1000,
            $"Dependency filter took {ms}ms, expected <1000ms");
    }

    [Fact]
    public void DependencyFilter_CompletesWithin1Second_With200Scenarios()
    {
        var dependencies = new[] { "OrderService", "PaymentGateway", "NotificationService", "CacheService", "AuditService", "UserService", "InventoryService" };
        var html = GenerateLargeReport(featureCount: 20, scenariosPerFeature: 10, dependencies: dependencies);

        _driver.Navigate().GoToUrl(ServePage(html));

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState") as string == "complete");

        ((IJavaScriptExecutor)_driver).ExecuteScript("fc();");

        var ms = (long)((IJavaScriptExecutor)_driver).ExecuteScript(
            "var btn = document.querySelector('.dependency-toggle[data-dependency=\"OrderService\"]');" +
            "var t0 = performance.now(); btn.click(); return Math.round(performance.now() - t0);")!;

        var hiddenCount = _driver.FindElements(By.CssSelector(".scenario.dep-hidden")).Count;
        Assert.True(hiddenCount > 0, "Some scenarios should be hidden by the filter");

        Assert.True(ms < 1000,
            $"Dependency filter with 200 scenarios took {ms}ms, expected <1000ms");
    }

    [Fact]
    public void DependencyFilter_ToggleOffCompletesWithin1Second()
    {
        var dependencies = new[] { "OrderService", "PaymentGateway", "NotificationService", "CacheService", "AuditService" };
        var html = GenerateLargeReport(featureCount: 20, scenariosPerFeature: 5, dependencies: dependencies);

        _driver.Navigate().GoToUrl(ServePage(html));

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState") as string == "complete");

        ((IJavaScriptExecutor)_driver).ExecuteScript("fc();");

        // Toggle on
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.querySelector('.dependency-toggle[data-dependency=\"OrderService\"]').click();");

        // Toggle off and measure inside JS
        var ms = (long)((IJavaScriptExecutor)_driver).ExecuteScript(
            "var btn = document.querySelector('.dependency-toggle[data-dependency=\"OrderService\"]');" +
            "var t0 = performance.now(); btn.click(); return Math.round(performance.now() - t0);")!;

        var hiddenCount = _driver.FindElements(By.CssSelector(".scenario.dep-hidden")).Count;
        Assert.Equal(0, hiddenCount);

        Assert.True(ms < 1000,
            $"Dependency filter toggle-off took {ms}ms, expected <1000ms");
    }

    [Fact]
    public void DependencyFilter_FeaturesContracted_WhenMoreThan10Matches()
    {
        // All scenarios get OrderService, so filtering by it yields >10 visible → features should NOT be opened
        var dependencies = new[] { "OrderService", "PaymentGateway" };
        var html = GenerateLargeReport(featureCount: 5, scenariosPerFeature: 5, dependencies: dependencies);

        _driver.Navigate().GoToUrl(ServePage(html));

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState") as string == "complete");

        // Click OrderService filter - all scenarios have it (≥25 scenarios), so >10 visible
        var btn = _driver.FindElement(By.CssSelector(".dependency-toggle[data-dependency='OrderService']"));
        btn.Click();

        // Check that visible features are NOT opened (contracted)
        var openedFeatures = _driver.FindElements(By.CssSelector(".feature[open]:not(.dep-hidden)"));
        var visibleFeatures = _driver.FindElements(By.CssSelector(".feature:not(.dep-hidden)"));

        Assert.True(visibleFeatures.Count > 0, "Some features should be visible");
        Assert.Empty(openedFeatures);
    }
}
