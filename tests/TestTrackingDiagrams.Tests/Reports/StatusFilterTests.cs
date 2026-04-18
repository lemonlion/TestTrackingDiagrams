using AngleSharp;
using AngleSharp.Dom;

namespace TestTrackingDiagrams.Tests.Reports;

public class StatusFilterTests
{
    #region Helpers

    private static IDocument BuildDomWithStatuses(params FeatureSpec[] features)
    {
        var html = "<html><body>";
        html += "<div class=\"filters\">";
        html += "<div class=\"status-filters\">";
        html += "<span class=\"status-filters-label\">Status:</span>";
        html += "<button class=\"status-toggle\" data-status=\"Passed\">Passed</button>";
        html += "<button class=\"status-toggle\" data-status=\"Failed\">Failed</button>";
        html += "<button class=\"status-toggle\" data-status=\"Skipped\">Skipped</button>";
        html += "</div>";
        html += "</div>";

        foreach (var feature in features)
        {
            html += $"<details class=\"feature\">";
            html += $"<summary class=\"h2\">{feature.Name}</summary>";

            foreach (var scenario in feature.Scenarios)
            {
                var classes = "scenario" + (scenario.IsHappyPath ? " happy-path" : "");
                html += $"<details class=\"{classes}\" data-status=\"{scenario.Status}\">";
                html += $"<summary class=\"h3\">{scenario.Title}</summary>";
                html += "</details>";
            }

            html += "</details>";
        }

        html += "</body></html>";

        var context = BrowsingContext.New(Configuration.Default);
        return context.OpenAsync(req => req.Content(html)).Result;
    }

    private record FeatureSpec(string Name, ScenarioSpec[] Scenarios);
    private record ScenarioSpec(string Title, string Status = "Passed", bool IsHappyPath = false);

    private static bool IsVisible(IElement el) =>
        !el.ClassList.Contains("search-hidden") &&
        !el.ClassList.Contains("dep-hidden") &&
        !el.ClassList.Contains("status-hidden");

    #endregion

    #region No Toggles → No Filtering

    [Fact]
    public void NoStatusToggled_AllScenariosVisible()
    {
        var doc = BuildDomWithStatuses(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", "Passed"),
                new ScenarioSpec("Fail order", "Failed"),
                new ScenarioSpec("Skip order", "Skipped")
            ]));

        StatusFilter.Apply(doc, []);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.All(scenarios, s => Assert.True(IsVisible(s)));
    }

    #endregion

    #region Single Status Toggle

    [Fact]
    public void FilterPassed_OnlyPassedVisible()
    {
        var doc = BuildDomWithStatuses(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", "Passed"),
                new ScenarioSpec("Fail order", "Failed"),
                new ScenarioSpec("Skip order", "Skipped")
            ]));

        StatusFilter.Apply(doc, ["Passed"]);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]), "Passed should be visible");
        Assert.False(IsVisible(scenarios[1]), "Failed should be hidden");
        Assert.False(IsVisible(scenarios[2]), "Skipped should be hidden");
    }

    [Fact]
    public void FilterFailed_OnlyFailedVisible()
    {
        var doc = BuildDomWithStatuses(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", "Passed"),
                new ScenarioSpec("Fail order", "Failed")
            ]));

        StatusFilter.Apply(doc, ["Failed"]);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.False(IsVisible(scenarios[0]), "Passed should be hidden");
        Assert.True(IsVisible(scenarios[1]), "Failed should be visible");
    }

    #endregion

    #region Multiple Status Toggles (OR logic)

    [Fact]
    public void FilterPassedAndFailed_SkippedHidden()
    {
        var doc = BuildDomWithStatuses(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", "Passed"),
                new ScenarioSpec("Fail order", "Failed"),
                new ScenarioSpec("Skip order", "Skipped")
            ]));

        StatusFilter.Apply(doc, ["Passed", "Failed"]);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]), "Passed should be visible");
        Assert.True(IsVisible(scenarios[1]), "Failed should be visible");
        Assert.False(IsVisible(scenarios[2]), "Skipped should be hidden");
    }

    #endregion

    #region Feature Visibility

    [Fact]
    public void StatusFilter_HidesFeaturesWithNoVisibleScenarios()
    {
        var doc = BuildDomWithStatuses(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", "Passed")
            ]),
            new FeatureSpec("Users", [
                new ScenarioSpec("Fail user", "Failed")
            ]));

        StatusFilter.Apply(doc, ["Passed"]);

        var features = doc.QuerySelectorAll(".feature");
        Assert.True(IsVisible(features[0]), "Orders feature has passing scenario");
        Assert.False(IsVisible(features[1]), "Users feature has only failed scenario");
    }

    [Fact]
    public void StatusFilter_OpensFeaturesWithVisibleScenarios()
    {
        var doc = BuildDomWithStatuses(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Fail order", "Failed")
            ]));

        StatusFilter.Apply(doc, ["Failed"]);

        var feature = doc.QuerySelector(".feature")!;
        Assert.True(feature.HasAttribute("open"));
    }

    #endregion

    #region Clear Filter

    [Fact]
    public void ClearStatusFilter_AllVisibleAgain()
    {
        var doc = BuildDomWithStatuses(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", "Passed"),
                new ScenarioSpec("Fail order", "Failed")
            ]));

        StatusFilter.Apply(doc, ["Passed"]);
        StatusFilter.Apply(doc, []);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.All(scenarios, s => Assert.True(IsVisible(s)));
    }

    #endregion

    #region Compatibility with Other Filters

    [Fact]
    public void StatusFilter_DoesNotInterfereWithSearchHidden()
    {
        var doc = BuildDomWithStatuses(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", "Passed"),
                new ScenarioSpec("Update order", "Passed")
            ]));

        doc.QuerySelectorAll(".scenario")[1].ClassList.Add("search-hidden");
        StatusFilter.Apply(doc, ["Passed"]);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]));
        Assert.False(IsVisible(scenarios[1]), "search-hidden should still apply");
    }

    [Fact]
    public void StatusFilter_DoesNotInterfereWithDepHidden()
    {
        var doc = BuildDomWithStatuses(
            new FeatureSpec("Orders", [
                new ScenarioSpec("Create order", "Passed"),
                new ScenarioSpec("Update order", "Passed")
            ]));

        doc.QuerySelectorAll(".scenario")[1].ClassList.Add("dep-hidden");
        StatusFilter.Apply(doc, ["Passed"]);

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]));
        Assert.False(IsVisible(scenarios[1]), "dep-hidden should still apply");
    }

    #endregion

    #region Feature Contraction when >10 Visible Scenarios

    [Fact]
    public void StatusFilter_FeaturesContracted_WhenMoreThan10VisibleScenarios()
    {
        var scenarios = Enumerable.Range(1, 12)
            .Select(i => new ScenarioSpec($"Scenario {i}", "Passed"))
            .ToArray();

        var doc = BuildDomWithStatuses(
            new FeatureSpec("Feature A", scenarios[..6]),
            new FeatureSpec("Feature B", scenarios[6..]));

        StatusFilter.Apply(doc, ["Passed"]);

        var features = doc.QuerySelectorAll(".feature");
        Assert.False(features[0].HasAttribute("open"), "Feature should stay contracted when >10 visible scenarios");
        Assert.False(features[1].HasAttribute("open"), "Feature should stay contracted when >10 visible scenarios");
    }

    [Fact]
    public void StatusFilter_FeaturesExpanded_WhenAtMost10VisibleScenarios()
    {
        var scenarios = Enumerable.Range(1, 10)
            .Select(i => new ScenarioSpec($"Scenario {i}", "Passed"))
            .ToArray();

        var doc = BuildDomWithStatuses(
            new FeatureSpec("Feature A", scenarios[..5]),
            new FeatureSpec("Feature B", scenarios[5..]));

        StatusFilter.Apply(doc, ["Passed"]);

        var features = doc.QuerySelectorAll(".feature");
        Assert.True(features[0].HasAttribute("open"), "Feature should open when ≤10 visible scenarios");
        Assert.True(features[1].HasAttribute("open"), "Feature should open when ≤10 visible scenarios");
    }

    #endregion
}
