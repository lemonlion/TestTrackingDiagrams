using AngleSharp;
using AngleSharp.Dom;

namespace TestTrackingDiagrams.Tests.Reports;

public class DependencyFilterTests
{
    #region Helpers

    private static IDocument BuildReportDomWithDependencies(params FeatureSpec[] features)
    {
        // Collect all unique dependencies across all scenarios
        var allDeps = new HashSet<string>();
        foreach (var f in features)
        foreach (var s in f.Scenarios)
        {
            if (s.Dependencies != null)
                foreach (var d in s.Dependencies)
                    allDeps.Add(d);
        }

        var html = "<html><body>";
        html += "<div class=\"filters\">";
        html += "<div><input id=\"searchbar\" placeholder=\"Search\" /></div>";
        html += "<div class=\"happy-path-filters\"><span class=\"happy-path-filters-label\">Happy Paths:</span><button class=\"happy-path-toggle\">Happy Paths Only</button></div>";

        // Dependency toggles
        if (allDeps.Count > 0)
        {
            html += "<div class=\"dependency-filters\">";
            html += "<span class=\"dependency-filters-label\">Dependencies:</span>";
            foreach (var dep in allDeps.OrderBy(d => d))
            {
                html += $"<button class=\"dependency-toggle\" data-dependency=\"{dep}\">{dep}</button>";
            }
            html += "</div>";
        }

        html += "</div>";

        foreach (var feature in features)
        {
            var featureOpen = feature.InitiallyOpen ? " open" : "";
            html += $"<details class=\"feature\"{featureOpen}>";
            html += $"<summary class=\"h2\">{feature.Name}</summary>";

            foreach (var scenario in feature.Scenarios)
            {
                var classes = "scenario" + (scenario.IsHappyPath ? " happy-path" : "");
                var depsAttr = scenario.Dependencies is { Length: > 0 }
                    ? $" data-dependencies=\"{string.Join(",", scenario.Dependencies)}\""
                    : "";
                html += $"<details class=\"{classes}\"{depsAttr}>";
                html += $"<summary class=\"h3\">{scenario.Title}</summary>";
                html += "</details>"; // scenario
            }

            html += "</details>"; // feature
        }

        html += "</body></html>";

        var context = BrowsingContext.New(Configuration.Default);
        return context.OpenAsync(req => req.Content(html)).Result;
    }

    private record FeatureSpec(string Name, ScenarioSpec[] Scenarios, bool InitiallyOpen = false);

    private record ScenarioSpec(
        string Title,
        string[]? Dependencies = null,
        bool IsHappyPath = false);

    private static bool IsVisible(IElement element) =>
        !element.ClassList.Contains("search-hidden") &&
        !element.ClassList.Contains("dep-hidden");

    #endregion

    #region No Toggles Active → No Filtering

    [Fact]
    public void NoDependenciesToggled_AllScenariosVisible()
    {
        var doc = BuildReportDomWithDependencies(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", Dependencies: ["OrderService", "PaymentGateway"]),
                new ScenarioSpec("Cancel order", Dependencies: ["OrderService"]),
                new ScenarioSpec("List orders", Dependencies: ["OrderService", "CacheService"])
            ]));

        DependencyFilter.Apply(doc, []);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.All(scenarios, s => Assert.True(IsVisible(s)));
    }

    #endregion

    #region Single Toggle → Filter to Matching Scenarios

    [Fact]
    public void SingleDependencyToggled_OnlyMatchingScenariosVisible()
    {
        var doc = BuildReportDomWithDependencies(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", Dependencies: ["OrderService", "PaymentGateway"]),
                new ScenarioSpec("Cancel order", Dependencies: ["OrderService"]),
                new ScenarioSpec("Check payment", Dependencies: ["PaymentGateway"])
            ]));

        DependencyFilter.Apply(doc, ["PaymentGateway"]);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]), "Create order has PaymentGateway");
        Assert.False(IsVisible(scenarios[1]), "Cancel order lacks PaymentGateway");
        Assert.True(IsVisible(scenarios[2]), "Check payment has PaymentGateway");
    }

    #endregion

    #region Multiple Toggles → AND Filter

    [Fact]
    public void MultipleDependenciesToggled_OnlyScenariosWithAllDepsVisible()
    {
        var doc = BuildReportDomWithDependencies(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", Dependencies: ["OrderService", "PaymentGateway", "NotificationService"]),
                new ScenarioSpec("Cancel order", Dependencies: ["OrderService", "NotificationService"]),
                new ScenarioSpec("Check payment", Dependencies: ["PaymentGateway"])
            ]));

        DependencyFilter.Apply(doc, ["OrderService", "PaymentGateway"]);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]), "Create order has both");
        Assert.False(IsVisible(scenarios[1]), "Cancel order lacks PaymentGateway");
        Assert.False(IsVisible(scenarios[2]), "Check payment lacks OrderService");
    }

    #endregion

    #region Feature Visibility

    [Fact]
    public void DependencyFilter_HidesFeaturesWithNoVisibleScenarios()
    {
        var doc = BuildReportDomWithDependencies(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", Dependencies: ["OrderService", "PaymentGateway"])
            ]),
            new FeatureSpec("Users", [
                new ScenarioSpec("Create user", Dependencies: ["UserService"])
            ]));

        DependencyFilter.Apply(doc, ["PaymentGateway"]);

        var features = doc.QuerySelectorAll(".feature");
        Assert.True(IsVisible(features[0]), "Orders feature has matching scenario");
        Assert.False(IsVisible(features[1]), "Users feature has no matching scenario");
    }

    [Fact]
    public void DependencyFilter_OpensFeaturesWithVisibleScenarios()
    {
        var doc = BuildReportDomWithDependencies(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", Dependencies: ["OrderService", "PaymentGateway"])
            ]));

        DependencyFilter.Apply(doc, ["PaymentGateway"]);

        var feature = doc.QuerySelector(".feature")!;
        Assert.True(feature.HasAttribute("open"), "Feature with visible scenarios should be opened");
    }

    #endregion

    #region Clear Filter → All Visible Again

    [Fact]
    public void ClearDependencyFilter_AllScenariosVisibleAgain()
    {
        var doc = BuildReportDomWithDependencies(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", Dependencies: ["OrderService", "PaymentGateway"]),
                new ScenarioSpec("Cancel order", Dependencies: ["OrderService"])
            ]));

        // Apply filter
        DependencyFilter.Apply(doc, ["PaymentGateway"]);
        // Clear filter
        DependencyFilter.Apply(doc, []);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.All(scenarios, s => Assert.True(IsVisible(s)));
    }

    [Fact]
    public void ClearDependencyFilter_AllFeaturesVisibleAgain()
    {
        var doc = BuildReportDomWithDependencies(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", Dependencies: ["OrderService"])
            ]),
            new FeatureSpec("Users", [
                new ScenarioSpec("Create user", Dependencies: ["UserService"])
            ]));

        DependencyFilter.Apply(doc, ["OrderService"]);
        DependencyFilter.Apply(doc, []);

        var features = doc.QuerySelectorAll(".feature");
        Assert.All(features, f => Assert.True(IsVisible(f)));
    }

    #endregion

    #region Compatibility with Search

    [Fact]
    public void DependencyFilter_DoesNotInterfereWithSearchHidden()
    {
        var doc = BuildReportDomWithDependencies(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", Dependencies: ["OrderService", "PaymentGateway"]),
                new ScenarioSpec("Cancel order", Dependencies: ["OrderService"])
            ]));

        // Simulate search hiding the second scenario
        doc.QuerySelectorAll(".scenario")[1].ClassList.Add("search-hidden");

        // Apply dependency filter that would show both
        DependencyFilter.Apply(doc, ["OrderService"]);

        var scenarios = doc.QuerySelectorAll(".scenario");
        // First scenario: visible (matches dep, not search-hidden)
        Assert.True(IsVisible(scenarios[0]));
        // Second scenario: still hidden by search, even though dep matches
        Assert.False(IsVisible(scenarios[1]), "search-hidden should still be in effect");
    }

    [Fact]
    public void DependencyFilterAndSearch_BothCanHideScenarios()
    {
        var doc = BuildReportDomWithDependencies(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", Dependencies: ["OrderService", "PaymentGateway"]),
                new ScenarioSpec("Cancel order", Dependencies: ["OrderService"]),
                new ScenarioSpec("Check payment", Dependencies: ["PaymentGateway"])
            ]));

        // Dependency filter hides "Check payment" (no OrderService)
        DependencyFilter.Apply(doc, ["OrderService"]);

        // Search hides "Cancel order" (doesn't contain "create")
        // Simulate search adding search-hidden to non-matching
        doc.QuerySelectorAll(".scenario")[1].ClassList.Add("search-hidden");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]), "Create order matches both filters");
        Assert.False(IsVisible(scenarios[1]), "Cancel order hidden by search");
        Assert.False(IsVisible(scenarios[2]), "Check payment hidden by dep filter");
    }

    #endregion

    #region Scenarios without Dependencies

    [Fact]
    public void ScenariosWithoutDependencies_HiddenWhenFilterActive()
    {
        var doc = BuildReportDomWithDependencies(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", Dependencies: ["OrderService"]),
                new ScenarioSpec("Manual test") // no dependencies
            ]));

        DependencyFilter.Apply(doc, ["OrderService"]);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]));
        Assert.False(IsVisible(scenarios[1]), "Scenario without deps should be hidden when filter active");
    }

    [Fact]
    public void ScenariosWithoutDependencies_VisibleWhenNoFilterActive()
    {
        var doc = BuildReportDomWithDependencies(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", Dependencies: ["OrderService"]),
                new ScenarioSpec("Manual test") // no dependencies
            ]));

        DependencyFilter.Apply(doc, []);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.All(scenarios, s => Assert.True(IsVisible(s)));
    }

    #endregion
}
