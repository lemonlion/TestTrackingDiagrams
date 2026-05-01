using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using TestTrackingDiagrams.Reports;
using static TestTrackingDiagrams.DefaultDiagramsFetcher;

namespace TestTrackingDiagrams.Tests.Selenium;

public class FailureClusterLinkTests : IClassFixture<ChromeFixture>, IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(FailureClusterLinkTests).Assembly.Location)!,
        "SeleniumOutput");

    private const string PlantUmlSource = """
        @startuml
        actor "Caller" as caller
        participant "Service" as svc
        caller -> svc : GET /api/test
        svc --> caller : 200 OK
        @enduml
        """;

    public FailureClusterLinkTests(ChromeFixture chrome)
    {
        _driver = chrome.Driver;
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-cluster-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    private string GenerateClusterReport(string fileName, Feature[] features, DiagramAsCode[]? diagrams = null)
    {
        diagrams ??= features
            .SelectMany(f => f.Scenarios)
            .Select(s => new DiagramAsCode(s.Id, "", PlantUmlSource))
            .ToArray();

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(_tempDir, fileName), "Cluster Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(OutputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    /// <summary>
    /// Two failed scenarios in the same feature sharing the same error message — cluster link should scroll to each.
    /// </summary>
    private Feature[] CreateBasicClusterData() =>
    [
        new Feature
        {
            DisplayName = "Order Feature",
            Scenarios =
            [
                new Scenario
                {
                    Id = "c1", DisplayName = "Create order fails on timeout",
                    Result = ExecutionResult.Failed, Duration = TimeSpan.FromSeconds(5),
                    ErrorMessage = "Connection refused (Stock Service:5001)"
                },
                new Scenario
                {
                    Id = "c2", DisplayName = "Update order fails on timeout",
                    Result = ExecutionResult.Failed, Duration = TimeSpan.FromSeconds(3),
                    ErrorMessage = "Connection refused (Stock Service:5001)"
                },
                new Scenario
                {
                    Id = "c3", DisplayName = "List orders succeeds",
                    Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1),
                    IsHappyPath = true
                }
            ]
        }
    ];

    /// <summary>
    /// Failed scenarios spread across multiple features — cluster link must open the correct feature.
    /// </summary>
    private Feature[] CreateMultiFeatureClusterData() =>
    [
        new Feature
        {
            DisplayName = "Order Feature",
            Scenarios =
            [
                new Scenario
                {
                    Id = "mf1", DisplayName = "Create order fails",
                    Result = ExecutionResult.Failed, Duration = TimeSpan.FromSeconds(5),
                    ErrorMessage = "Database connection timeout after 30s"
                },
                new Scenario
                {
                    Id = "mf2", DisplayName = "List orders succeeds",
                    Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1),
                    IsHappyPath = true
                }
            ]
        },
        new Feature
        {
            DisplayName = "Payment Feature",
            Scenarios =
            [
                new Scenario
                {
                    Id = "mf3", DisplayName = "Process payment fails",
                    Result = ExecutionResult.Failed, Duration = TimeSpan.FromSeconds(4),
                    ErrorMessage = "Database connection timeout after 30s"
                },
                new Scenario
                {
                    Id = "mf4", DisplayName = "Refund succeeds",
                    Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1),
                    IsHappyPath = true
                }
            ]
        },
        new Feature
        {
            DisplayName = "Shipping Feature",
            Scenarios =
            [
                new Scenario
                {
                    Id = "mf5", DisplayName = "Calculate shipping fails",
                    Result = ExecutionResult.Failed, Duration = TimeSpan.FromSeconds(3),
                    ErrorMessage = "Database connection timeout after 30s"
                },
                new Scenario
                {
                    Id = "mf6", DisplayName = "Free shipping applied",
                    Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1),
                    IsHappyPath = true
                }
            ]
        }
    ];

    /// <summary>
    /// Parameterized scenarios sharing an error message — link targets a TR row.
    /// </summary>
    private Feature[] CreateParameterizedClusterData() =>
    [
        new Feature
        {
            DisplayName = "Checkout Feature",
            Scenarios =
            [
                new Scenario
                {
                    Id = "p1", DisplayName = "Checkout completes (UK, GBP)",
                    Result = ExecutionResult.Failed, Duration = TimeSpan.FromSeconds(2),
                    ErrorMessage = "Payment gateway unavailable",
                    ExampleValues = new Dictionary<string, string> { ["Country"] = "UK", ["Currency"] = "GBP" },
                    ExampleRawValues = new Dictionary<string, object?> { ["Country"] = "UK", ["Currency"] = "GBP" }
                },
                new Scenario
                {
                    Id = "p2", DisplayName = "Checkout completes (US, USD)",
                    Result = ExecutionResult.Failed, Duration = TimeSpan.FromSeconds(2),
                    ErrorMessage = "Payment gateway unavailable",
                    ExampleValues = new Dictionary<string, string> { ["Country"] = "US", ["Currency"] = "USD" },
                    ExampleRawValues = new Dictionary<string, object?> { ["Country"] = "US", ["Currency"] = "USD" }
                },
                new Scenario
                {
                    Id = "p3", DisplayName = "Checkout completes (DE, EUR)",
                    Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1),
                    ExampleValues = new Dictionary<string, string> { ["Country"] = "DE", ["Currency"] = "EUR" },
                    ExampleRawValues = new Dictionary<string, object?> { ["Country"] = "DE", ["Currency"] = "EUR" }
                }
            ]
        }
    ];

    /// <summary>
    /// Two different clusters — scenarios with duplicate display names across features.
    /// </summary>
    private Feature[] CreateDuplicateNameClusterData() =>
    [
        new Feature
        {
            DisplayName = "Order API",
            Scenarios =
            [
                new Scenario
                {
                    Id = "dn1", DisplayName = "Health check fails",
                    Result = ExecutionResult.Failed, Duration = TimeSpan.FromSeconds(1),
                    ErrorMessage = "Service unavailable (503)"
                },
                new Scenario
                {
                    Id = "dn2", DisplayName = "Normal operation succeeds",
                    Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1),
                    IsHappyPath = true
                }
            ]
        },
        new Feature
        {
            DisplayName = "Payment API",
            Scenarios =
            [
                new Scenario
                {
                    Id = "dn3", DisplayName = "Health check fails",
                    Result = ExecutionResult.Failed, Duration = TimeSpan.FromSeconds(1),
                    ErrorMessage = "Service unavailable (503)"
                },
                new Scenario
                {
                    Id = "dn4", DisplayName = "Payment processed",
                    Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1),
                    IsHappyPath = true
                }
            ]
        }
    ];

    /// <summary>
    /// Multiple clusters where we navigate between them sequentially.
    /// </summary>
    private Feature[] CreateMultiClusterData() =>
    [
        new Feature
        {
            DisplayName = "API Feature",
            Scenarios =
            [
                new Scenario
                {
                    Id = "mc1", DisplayName = "Endpoint A timeout",
                    Result = ExecutionResult.Failed,
                    ErrorMessage = "Connection refused (Stock Service:5001)"
                },
                new Scenario
                {
                    Id = "mc2", DisplayName = "Endpoint B timeout",
                    Result = ExecutionResult.Failed,
                    ErrorMessage = "Connection refused (Stock Service:5001)"
                },
                new Scenario
                {
                    Id = "mc3", DisplayName = "Endpoint C auth error",
                    Result = ExecutionResult.Failed,
                    ErrorMessage = "Unauthorized: token expired"
                },
                new Scenario
                {
                    Id = "mc4", DisplayName = "Endpoint D auth error",
                    Result = ExecutionResult.Failed,
                    ErrorMessage = "Unauthorized: token expired"
                },
                new Scenario
                {
                    Id = "mc5", DisplayName = "Endpoint E works",
                    Result = ExecutionResult.Passed, IsHappyPath = true
                }
            ]
        }
    ];

    private bool IsElementInViewport(IWebElement element)
    {
        return (bool)((IJavaScriptExecutor)_driver).ExecuteScript("""
            var el = arguments[0];
            var rect = el.getBoundingClientRect();
            return rect.top >= -10 && rect.top < window.innerHeight && rect.bottom > 0;
            """, element)!;
    }

    private void WaitForScrollToSettle()
    {
        Thread.Sleep(800);
    }

    private IWebElement WaitFor(By by, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    // ── Basic cluster link navigation ──

    [Fact]
    public void Cluster_link_scrolls_to_correct_scenario()
    {
        var url = GenerateClusterReport("ClusterBasicScroll.html", CreateBasicClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        // Open the cluster details
        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();

        // Click the first cluster link
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        Assert.True(links.Count >= 2);
        links[0].Click();
        WaitForScrollToSettle();

        // The target scenario should be visible
        var targetScenario = _driver.FindElement(By.Id("scenario-create-order-fails-on-timeout"));
        Assert.True(IsElementInViewport(targetScenario), "Target scenario should be in viewport after cluster link click");
    }

    [Fact]
    public void Cluster_link_opens_scenario_details()
    {
        var url = GenerateClusterReport("ClusterOpensScenario.html", CreateBasicClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();

        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        links[0].Click();
        WaitForScrollToSettle();

        var targetScenario = _driver.FindElement(By.Id("scenario-create-order-fails-on-timeout"));
        Assert.NotNull(targetScenario.GetAttribute("open"));
    }

    [Fact]
    public void Cluster_link_opens_parent_feature()
    {
        var url = GenerateClusterReport("ClusterOpensFeature.html", CreateBasicClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        // Verify feature is initially closed
        var feature = _driver.FindElement(By.CssSelector("details.feature"));
        Assert.Null(feature.GetAttribute("open"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        links[0].Click();
        WaitForScrollToSettle();

        // Feature should now be open
        Assert.NotNull(feature.GetAttribute("open"));
    }

    // ── Multi-feature navigation ──

    [Fact]
    public void Cluster_link_navigates_to_scenario_in_second_feature()
    {
        var url = GenerateClusterReport("ClusterSecondFeature.html", CreateMultiFeatureClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();

        // Click the second link (which is in the Payment Feature)
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        Assert.True(links.Count >= 2);
        links[1].Click();
        WaitForScrollToSettle();

        var targetScenario = _driver.FindElement(By.Id("scenario-process-payment-fails"));
        Assert.True(IsElementInViewport(targetScenario),
            "Scenario in second feature should be in viewport");
        Assert.NotNull(targetScenario.GetAttribute("open"));
    }

    [Fact]
    public void Cluster_link_navigates_to_scenario_in_third_feature()
    {
        var url = GenerateClusterReport("ClusterThirdFeature.html", CreateMultiFeatureClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();

        // Click the third link (in Shipping Feature)
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        Assert.True(links.Count >= 3);
        links[2].Click();
        WaitForScrollToSettle();

        var targetScenario = _driver.FindElement(By.Id("scenario-calculate-shipping-fails"));
        Assert.True(IsElementInViewport(targetScenario),
            "Scenario in third feature should be in viewport");
    }

    // ── Sequential navigation (clicking multiple cluster links in a row) ──

    [Fact]
    public void Clicking_second_cluster_link_scrolls_to_second_scenario()
    {
        var url = GenerateClusterReport("ClusterSequentialNav.html", CreateMultiFeatureClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));

        // Click first link
        links[0].Click();
        WaitForScrollToSettle();

        var firstTarget = _driver.FindElement(By.Id("scenario-create-order-fails"));
        Assert.True(IsElementInViewport(firstTarget), "First target should be in viewport");

        // Scroll back to top to simulate user returning to cluster section
        ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0,0)");
        Thread.Sleep(300);

        // Click second link
        links[1].Click();
        WaitForScrollToSettle();

        var secondTarget = _driver.FindElement(By.Id("scenario-process-payment-fails"));
        Assert.True(IsElementInViewport(secondTarget), "Second target should be in viewport after clicking second link");
    }

    [Fact]
    public void Sequential_cluster_link_clicks_without_scrolling_back()
    {
        var url = GenerateClusterReport("ClusterSequentialDirect.html", CreateMultiFeatureClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));

        // Click first link
        links[0].Click();
        WaitForScrollToSettle();

        // Without scrolling back, click second link (which is still visible if cluster stays expanded at top)
        // Use JavaScript to click since element might be off-screen
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", links[1]);
        WaitForScrollToSettle();

        var secondTarget = _driver.FindElement(By.Id("scenario-process-payment-fails"));
        Assert.True(IsElementInViewport(secondTarget),
            "Second target should be in viewport even when clicking link without scrolling back first");
    }

    // ── Parameterized scenario navigation ──

    [Fact]
    public void Cluster_link_navigates_to_parameterized_row()
    {
        var url = GenerateClusterReport("ClusterParamRow.html", CreateParameterizedClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        Assert.True(links.Count >= 2);

        // Click the first parameterized failure
        links[0].Click();
        WaitForScrollToSettle();

        // The parameterized group's parent details should be opened
        var paramGroup = _driver.FindElement(By.CssSelector("details.scenario-parameterized"));
        Assert.NotNull(paramGroup.GetAttribute("open"));
    }

    [Fact]
    public void Cluster_link_activates_correct_parameterized_row()
    {
        var url = GenerateClusterReport("ClusterParamActive.html", CreateParameterizedClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));

        // Click the second link (US, USD)
        links[1].Click();
        WaitForScrollToSettle();

        // The clicked row should be active
        var targetRow = _driver.FindElement(By.Id("scenario-checkout-completes-us-usd"));
        Assert.Contains("row-active", targetRow.GetAttribute("class"));
    }

    [Fact]
    public void Cluster_link_to_parameterized_row_shows_correct_detail_panel()
    {
        var url = GenerateClusterReport("ClusterParamPanel.html", CreateParameterizedClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));

        // Click second parameterized link (US, USD — row index 1)
        links[1].Click();
        WaitForScrollToSettle();

        // The detail panel for the clicked row should be visible
        var activePanel = ((IJavaScriptExecutor)_driver).ExecuteScript("""
            var row = document.getElementById('scenario-checkout-completes-us-usd');
            if (!row) return 'row-not-found';
            var idx = row.getAttribute('data-row-idx');
            // Extract the prefix from the row's onclick attribute (e.g. "selectRow(this,'pgrp0')")
            var onclick = row.getAttribute('onclick');
            if (!onclick) return 'no-onclick';
            var match = onclick.match(/selectRow\(this,'([^']+)'\)/);
            if (!match) return 'no-prefix-match';
            var prefix = match[1];
            var panel = document.getElementById(prefix + '-detail-' + idx);
            if (!panel) return 'panel-not-found:' + prefix + '-detail-' + idx;
            return panel.style.display !== 'none' ? 'visible' : 'hidden';
            """);
        Assert.Equal("visible", activePanel);
    }

    // ── Duplicate scenario names across features ──

    [Fact]
    public void Cluster_link_with_duplicate_names_navigates_to_correct_feature()
    {
        var url = GenerateClusterReport("ClusterDuplicateNames.html", CreateDuplicateNameClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));

        // Both links point to "Health check fails" — but in different features
        // The second link should navigate to the Payment API feature's scenario (with deduplicated ID)
        Assert.True(links.Count >= 2);

        // Click the second link (Payment API > Health check fails)
        links[1].Click();
        WaitForScrollToSettle();

        // The second feature (Payment API) should be open
        var features = _driver.FindElements(By.CssSelector("details.feature"));
        Assert.True(features.Count >= 2);
        Assert.True(features[1].GetAttribute("open") is not null,
            "Second feature should be opened when clicking the cluster link for the second occurrence");

        // Verify the correct scenario element has the deduplicated ID
        var secondScenario = _driver.FindElement(By.Id("scenario-health-check-fails-2"));
        Assert.NotNull(secondScenario);
        Assert.True(IsElementInViewport(secondScenario), "Second occurrence of duplicate-named scenario should be in viewport");
    }

    // ── URL hash update ──

    [Fact]
    public void Cluster_link_updates_url_hash()
    {
        var url = GenerateClusterReport("ClusterUrlHash.html", CreateBasicClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        links[0].Click();
        WaitForScrollToSettle();

        var currentUrl = _driver.Url;
        Assert.Contains("#scenario-create-order-fails-on-timeout", currentUrl);
    }

    // ── Multi-cluster navigation ──

    [Fact]
    public void Clicking_links_between_different_clusters()
    {
        var url = GenerateClusterReport("ClusterMultiCluster.html", CreateMultiClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        // Open both cluster details
        var clusterSummaries = _driver.FindElements(By.CssSelector(".failure-cluster > summary"));
        foreach (var s in clusterSummaries) s.Click();

        var allLinks = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        Assert.True(allLinks.Count >= 4);

        // Click link from first cluster
        allLinks[0].Click();
        WaitForScrollToSettle();

        var firstTarget = _driver.FindElement(By.Id("scenario-endpoint-a-timeout"));
        Assert.True(IsElementInViewport(firstTarget));

        // Navigate back to top and click link from second cluster
        ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0,0)");
        Thread.Sleep(300);

        // Find links from second cluster
        allLinks[2].Click(); // Third link is in the second cluster (auth error)
        WaitForScrollToSettle();

        var secondTarget = _driver.FindElement(By.Id("scenario-endpoint-c-auth-error"));
        Assert.True(IsElementInViewport(secondTarget),
            "Should navigate to scenario in second cluster");
    }

    // ── Edge case: cluster link when feature is already open ──

    [Fact]
    public void Cluster_link_works_when_feature_already_expanded()
    {
        var url = GenerateClusterReport("ClusterFeatureAlreadyOpen.html", CreateBasicClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        // Expand the feature first
        _driver.FindElement(By.CssSelector("details.feature > summary")).Click();
        Thread.Sleep(200);

        // Now click a cluster link
        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        links[0].Click();
        WaitForScrollToSettle();

        var targetScenario = _driver.FindElement(By.Id("scenario-create-order-fails-on-timeout"));
        Assert.True(IsElementInViewport(targetScenario));
        Assert.NotNull(targetScenario.GetAttribute("open"));
    }

    // ── Verify cluster feature prefix is displayed ──

    [Fact]
    public void Cluster_links_show_feature_name_prefix()
    {
        var url = GenerateClusterReport("ClusterFeaturePrefix.html", CreateMultiFeatureClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();

        var prefixSpans = _driver.FindElements(By.CssSelector(".failure-cluster-scenarios li span"));
        Assert.True(prefixSpans.Count >= 2);
        Assert.Contains("Order Feature", prefixSpans[0].Text);
        Assert.Contains("Payment Feature", prefixSpans[1].Text);
    }

    // ── Parameterized group parent opening ──

    [Fact]
    public void Cluster_link_opens_parameterized_group_parent_details()
    {
        // This tests the real-world bug where a <tr> has a parent <details class="scenario-parameterized">
        // that also needs to be opened for the row to be visible.
        var url = GenerateClusterReport("ClusterParamParentOpen.html", CreateParameterizedClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        // Verify parameterized group is initially closed
        var paramGroup = _driver.FindElement(By.CssSelector("details.scenario-parameterized"));
        Assert.Null(paramGroup.GetAttribute("open"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        links[0].Click();
        WaitForScrollToSettle();

        // Both the feature AND the parameterized group should now be open
        var feature = _driver.FindElement(By.CssSelector("details.feature"));
        Assert.NotNull(feature.GetAttribute("open"));
        Assert.NotNull(paramGroup.GetAttribute("open"));
    }

    [Fact]
    public void Cluster_link_to_parameterized_row_scrolls_to_visible_row()
    {
        var url = GenerateClusterReport("ClusterParamVisible.html", CreateParameterizedClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        links[0].Click();
        WaitForScrollToSettle();

        // The target row should be visible in the viewport
        var targetRow = _driver.FindElement(By.Id("scenario-checkout-completes-uk-gbp"));
        Assert.True(IsElementInViewport(targetRow),
            "Parameterized row should be visible in viewport after cluster link click");
    }

    // ── Second click after first (the primary bug in the original report) ──

    [Fact]
    public void Second_cluster_link_works_after_first_click_without_manual_scroll()
    {
        // This is the exact bug scenario from the real report:
        // 1. Click first link -> works
        // 2. Click second link without scrolling back -> should still navigate correctly
        var url = GenerateClusterReport("ClusterSecondNoScroll.html", CreateMultiFeatureClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));

        // Click first link
        links[0].Click();
        WaitForScrollToSettle();

        var firstTarget = _driver.FindElement(By.Id("scenario-create-order-fails"));
        Assert.True(IsElementInViewport(firstTarget), "First link should work");

        // Now click second link via JS (simulating the cluster being off-screen)
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", links[1]);
        WaitForScrollToSettle();

        var secondTarget = _driver.FindElement(By.Id("scenario-process-payment-fails"));
        Assert.True(IsElementInViewport(secondTarget),
            "Second cluster link should navigate correctly without needing to scroll back first");
        Assert.NotNull(secondTarget.GetAttribute("open"));
    }

    [Fact]
    public void Third_cluster_link_works_after_first_two_clicks()
    {
        var url = GenerateClusterReport("ClusterThirdClick.html", CreateMultiFeatureClusterData());
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector(".failure-clusters"));

        _driver.FindElement(By.CssSelector(".failure-cluster > summary")).Click();
        var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
        Assert.True(links.Count >= 3);

        // Click all three in sequence
        links[0].Click();
        WaitForScrollToSettle();

        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", links[1]);
        WaitForScrollToSettle();

        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", links[2]);
        WaitForScrollToSettle();

        var thirdTarget = _driver.FindElement(By.Id("scenario-calculate-shipping-fails"));
        Assert.True(IsElementInViewport(thirdTarget),
            "Third cluster link should navigate correctly after two prior clicks");
    }
}
