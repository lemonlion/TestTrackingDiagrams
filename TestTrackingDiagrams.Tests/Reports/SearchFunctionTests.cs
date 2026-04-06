using AngleSharp;
using AngleSharp.Dom;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// Tests for the JavaScript search_scenarios behaviour in the standard FeaturesReport.
/// We use AngleSharp to create a DOM that mirrors the report HTML, then invoke
/// the C#-ported search logic (which must behave identically to the JS) to verify
/// visibility, expand/retract, and filter-compatibility rules.
/// </summary>
public class SearchFunctionTests
{
    #region Helpers

    /// <summary>
    /// Builds a minimal standard-report DOM containing the given features/scenarios.
    /// Each scenario can have a title, gherkin steps, plantuml code, and a description.
    /// </summary>
    private static IDocument BuildStandardReportDom(params FeatureSpec[] features)
    {
        var html = "<html><body>";
        html += "<div class=\"filters\">";
        html += "<div class=\"happy-path-filters\"><span class=\"happy-path-filters-label\">Happy Paths:</span><button class=\"happy-path-toggle\">Happy Paths Only</button></div>";
        html += "<div><input id=\"searchbar\" placeholder=\"Search\" /></div>";
        html += "</div>";

        foreach (var feature in features)
        {
            var featureOpen = feature.InitiallyOpen ? " open" : "";
            html += $"<details class=\"feature\"{featureOpen}>";
            html += $"<summary class=\"h2\">{feature.Name}</summary>";

            foreach (var scenario in feature.Scenarios)
            {
                var classes = "scenario" + (scenario.IsHappyPath ? " happy-path" : "");
                html += $"<details class=\"{classes}\">";
                html += $"<summary class=\"h3\">{scenario.Title}</summary>";

                if (scenario.Description != null)
                    html += $"<div class=\"scenario-description\">{scenario.Description}</div>";

                if (scenario.GherkinSteps != null)
                    html += $"<div class=\"scenario-steps\"><pre>{scenario.GherkinSteps}</pre></div>";

                if (scenario.PlantUml != null)
                {
                    html += "<details class=\"example-diagrams\" open>";
                    html += "<summary class=\"h4\">Sequence Diagrams</summary>";
                    html += "<details class=\"example\">";
                    html += "<summary class=\"example-image\"><img src=\"http://plantuml/png/test\"></summary>";
                    html += $"<div class=\"raw-plantuml\"><h4>Raw Plant UML</h4><pre>{scenario.PlantUml}</pre></div>";
                    html += "</details></details>";
                }

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
        string? Description = null,
        string? GherkinSteps = null,
        string? PlantUml = null,
        bool IsHappyPath = false);

    /// <summary>Determines if an element is "visible" — not hidden by display:none, .hide class, or search-hidden class.</summary>
    private static bool IsVisible(IElement element)
    {
        // Check inline display:none
        var style = element.GetAttribute("style") ?? "";
        if (style.Contains("display") && style.Contains("none"))
            return false;

        // Check .hide class (used by happy path filter)
        if (element.ClassList.Contains("hide"))
            return false;

        // Check search-hidden class (our new mechanism)
        if (element.ClassList.Contains("search-hidden"))
            return false;

        return true;
    }

    /// <summary>Checks if a details element is in expanded (open) state.</summary>
    private static bool IsExpanded(IElement detailsElement)
    {
        return detailsElement.HasAttribute("open");
    }

    #endregion

    #region Token Parsing

    [Fact]
    public void ParseSearchTokens_SingleWord_ReturnsSingleToken()
    {
        var tokens = SearchFunction.ParseSearchTokens("chocolate");
        Assert.Equal(["chocolate"], tokens);
    }

    [Fact]
    public void ParseSearchTokens_MultipleWords_ReturnsMultipleTokens()
    {
        var tokens = SearchFunction.ParseSearchTokens("chocolate cake");
        Assert.Equal(["chocolate", "cake"], tokens);
    }

    [Fact]
    public void ParseSearchTokens_QuotedPhrase_ReturnsPhraseAsOneToken()
    {
        var tokens = SearchFunction.ParseSearchTokens("\"chocolate cake\"");
        Assert.Equal(["chocolate cake"], tokens);
    }

    [Fact]
    public void ParseSearchTokens_MixedQuotedAndUnquoted_ReturnsCorrectTokens()
    {
        var tokens = SearchFunction.ParseSearchTokens("\"chocolate cake\" vanilla");
        Assert.Equal(["chocolate cake", "vanilla"], tokens);
    }

    [Fact]
    public void ParseSearchTokens_EmptyString_ReturnsEmpty()
    {
        var tokens = SearchFunction.ParseSearchTokens("");
        Assert.Empty(tokens);
    }

    [Fact]
    public void ParseSearchTokens_WhitespaceOnly_ReturnsEmpty()
    {
        var tokens = SearchFunction.ParseSearchTokens("   ");
        Assert.Empty(tokens);
    }

    [Fact]
    public void ParseSearchTokens_MultipleQuotedPhrases_ReturnsAllPhrases()
    {
        var tokens = SearchFunction.ParseSearchTokens("\"chocolate cake\" \"vanilla ice cream\"");
        Assert.Equal(["chocolate cake", "vanilla ice cream"], tokens);
    }

    #endregion

    #region Scenario Matching

    [Fact]
    public void ScenarioMatchesToken_InTitle_ReturnsTrue()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [new ScenarioSpec("Making Chocolate Cake")]));

        var scenario = doc.QuerySelector(".scenario")!;
        Assert.True(SearchFunction.ScenarioMatchesAllTokens(scenario, ["chocolate"]));
    }

    [Fact]
    public void ScenarioMatchesToken_InGherkinSteps_ReturnsTrue()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [new ScenarioSpec("Making Cake", GherkinSteps: "Given I have chocolate\nWhen I bake")]));

        var scenario = doc.QuerySelector(".scenario")!;
        Assert.True(SearchFunction.ScenarioMatchesAllTokens(scenario, ["chocolate"]));
    }

    [Fact]
    public void ScenarioMatchesToken_InPlantUml_ReturnsTrue()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking",
                [new ScenarioSpec("Making Cake", PlantUml: "@startuml\nChocolateService -> Oven\n@enduml")]));

        var scenario = doc.QuerySelector(".scenario")!;
        Assert.True(SearchFunction.ScenarioMatchesAllTokens(scenario, ["chocolateservice"]));
    }

    [Fact]
    public void ScenarioMatchesToken_InDescription_ReturnsTrue()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking",
                [new ScenarioSpec("Making Cake", Description: "This scenario tests the chocolate workflow")]));

        var scenario = doc.QuerySelector(".scenario")!;
        Assert.True(SearchFunction.ScenarioMatchesAllTokens(scenario, ["chocolate"]));
    }

    [Fact]
    public void ScenarioMatchesToken_CaseInsensitive_ReturnsTrue()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [new ScenarioSpec("Making CHOCOLATE Cake")]));

        var scenario = doc.QuerySelector(".scenario")!;
        Assert.True(SearchFunction.ScenarioMatchesAllTokens(scenario, ["chocolate"]));
    }

    [Fact]
    public void ScenarioMatchesToken_NoMatch_ReturnsFalse()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [new ScenarioSpec("Making Vanilla Cake")]));

        var scenario = doc.QuerySelector(".scenario")!;
        Assert.False(SearchFunction.ScenarioMatchesAllTokens(scenario, ["chocolate"]));
    }

    [Fact]
    public void ScenarioMatchesToken_AllTokensMustMatch()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [new ScenarioSpec("Making Chocolate Cake")]));

        var scenario = doc.QuerySelector(".scenario")!;
        Assert.True(SearchFunction.ScenarioMatchesAllTokens(scenario, ["chocolate", "cake"]));
        Assert.False(SearchFunction.ScenarioMatchesAllTokens(scenario, ["chocolate", "vanilla"]));
    }

    #endregion

    #region Multi-Match: Token appears in multiple scenarios

    [Fact]
    public void Search_MultipleMatches_OnlyMatchingScenariosVisible()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake"),
                new ScenarioSpec("Making Chocolate Brownies"),
                new ScenarioSpec("Making Vanilla Ice Cream")
            ]));

        SearchFunction.ApplySearch(doc, "chocolate");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]), "Chocolate Cake should be visible");
        Assert.True(IsVisible(scenarios[1]), "Chocolate Brownies should be visible");
        Assert.False(IsVisible(scenarios[2]), "Vanilla Ice Cream should not be visible");
    }

    [Fact]
    public void Search_MultipleMatches_ScenariosAreRetracted()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake"),
                new ScenarioSpec("Making Chocolate Brownies"),
                new ScenarioSpec("Making Vanilla Ice Cream")
            ]));

        SearchFunction.ApplySearch(doc, "chocolate");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.False(IsExpanded(scenarios[0]), "Chocolate Cake should be retracted");
        Assert.False(IsExpanded(scenarios[1]), "Chocolate Brownies should be retracted");
    }

    [Fact]
    public void Search_MultipleMatches_ParentFeatureIsVisible()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake"),
                new ScenarioSpec("Making Chocolate Brownies")
            ]));

        SearchFunction.ApplySearch(doc, "chocolate");

        var feature = doc.QuerySelector(".feature")!;
        Assert.True(IsVisible(feature), "Parent feature should be visible");
    }

    [Fact]
    public void Search_MultipleMatches_FeaturesWithNoMatchesAreHidden()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [new ScenarioSpec("Making Chocolate Cake")]),
            new FeatureSpec("Drinks", [new ScenarioSpec("Making Lemonade")]));

        SearchFunction.ApplySearch(doc, "chocolate");

        var features = doc.QuerySelectorAll(".feature");
        Assert.True(IsVisible(features[0]), "Baking feature should be visible");
        Assert.False(IsVisible(features[1]), "Drinks feature should not be visible");
    }

    [Fact]
    public void Search_MultipleMatches_AcrossFeatures_AllMatchingFeaturesVisible()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake"),
                new ScenarioSpec("Making Vanilla Cake")
            ]),
            new FeatureSpec("Drinks", [
                new ScenarioSpec("Hot Chocolate")
            ]));

        SearchFunction.ApplySearch(doc, "chocolate");

        var features = doc.QuerySelectorAll(".feature");
        Assert.True(IsVisible(features[0]), "Baking feature should be visible");
        Assert.True(IsVisible(features[1]), "Drinks feature should be visible");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]), "Chocolate Cake should be visible");
        Assert.False(IsVisible(scenarios[1]), "Vanilla Cake should not be visible");
        Assert.True(IsVisible(scenarios[2]), "Hot Chocolate should be visible");
    }

    #endregion

    #region Single Match: Token appears in exactly one scenario

    [Fact]
    public void Search_SingleMatch_ScenarioIsExpanded()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake", PlantUml: "@startuml\nA->B\n@enduml"),
                new ScenarioSpec("Making Vanilla Cake")
            ]));

        SearchFunction.ApplySearch(doc, "chocolate");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsExpanded(scenarios[0]), "Single matching scenario should be expanded");
    }

    [Fact]
    public void Search_SingleMatch_GherkinAndDiagramsVisible_RawPlantUmlNotVisible()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake",
                    GherkinSteps: "Given I have chocolate\nWhen I bake",
                    PlantUml: "@startuml\nA->B\n@enduml"),
                new ScenarioSpec("Making Vanilla Cake")
            ]));

        SearchFunction.ApplySearch(doc, "chocolate");

        var matchingScenario = doc.QuerySelectorAll(".scenario")[0];

        // The example-diagrams details should be open (diagrams visible)
        var diagramDetails = matchingScenario.QuerySelector("details.example-diagrams");
        if (diagramDetails != null)
            Assert.True(IsExpanded(diagramDetails), "Diagram details should be expanded");

        // The inner example details (containing raw plantuml) should NOT be open
        var exampleDetails = matchingScenario.QuerySelector("details.example");
        if (exampleDetails != null)
            Assert.False(IsExpanded(exampleDetails), "Raw PlantUML should not be expanded");
    }

    [Fact]
    public void Search_SingleMatch_NonMatchingScenariosHidden()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake"),
                new ScenarioSpec("Making Vanilla Cake")
            ]));

        SearchFunction.ApplySearch(doc, "chocolate");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]), "Chocolate Cake should be visible");
        Assert.False(IsVisible(scenarios[1]), "Vanilla Cake should not be visible");
    }

    #endregion

    #region Clear Search / Reset

    [Fact]
    public void Search_EmptyString_AllScenariosVisible()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake"),
                new ScenarioSpec("Making Vanilla Cake")
            ]));

        // First apply a search
        SearchFunction.ApplySearch(doc, "chocolate");
        // Then clear it
        SearchFunction.ApplySearch(doc, "");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]));
        Assert.True(IsVisible(scenarios[1]));
    }

    [Fact]
    public void Search_WhitespaceOnly_AllScenariosVisible()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake"),
                new ScenarioSpec("Making Vanilla Cake")
            ]));

        SearchFunction.ApplySearch(doc, "chocolate");
        SearchFunction.ApplySearch(doc, "   ");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]));
        Assert.True(IsVisible(scenarios[1]));
    }

    [Fact]
    public void Search_Clear_AllFeaturesVisible()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [new ScenarioSpec("Making Chocolate Cake")]),
            new FeatureSpec("Drinks", [new ScenarioSpec("Making Lemonade")]));

        SearchFunction.ApplySearch(doc, "chocolate");
        SearchFunction.ApplySearch(doc, "");

        var features = doc.QuerySelectorAll(".feature");
        Assert.True(IsVisible(features[0]));
        Assert.True(IsVisible(features[1]));
    }

    #endregion

    #region Happy Path Filter Compatibility

    [Fact]
    public void Search_WithHappyPathFilter_HiddenScenariosStayHidden()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake", IsHappyPath: true),
                new ScenarioSpec("Chocolate Error Path")
            ]));

        // Simulate happy path filter: add 'hide' class to non-happy-path scenarios
        var nonHappyScenarios = doc.QuerySelectorAll(".scenario:not(.happy-path)");
        foreach (var s in nonHappyScenarios)
            s.ClassList.Add("hide");

        SearchFunction.ApplySearch(doc, "chocolate");

        var scenarios = doc.QuerySelectorAll(".scenario");
        // Happy path chocolate scenario should be visible
        Assert.True(IsVisible(scenarios[0]), "Happy path chocolate should be visible");
        // Non-happy path is hidden by happy path filter - should remain hidden
        Assert.False(IsVisible(scenarios[1]), "Non-happy path should stay hidden by happy path filter");
    }

    [Fact]
    public void Search_Clear_HappyPathFilterStillApplied()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake", IsHappyPath: true),
                new ScenarioSpec("Chocolate Error Path")
            ]));

        // Apply happy path filter
        var nonHappyScenarios = doc.QuerySelectorAll(".scenario:not(.happy-path)");
        foreach (var s in nonHappyScenarios)
            s.ClassList.Add("hide");

        // Search then clear
        SearchFunction.ApplySearch(doc, "chocolate");
        SearchFunction.ApplySearch(doc, "");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]), "Happy path should be visible");
        // The hide class from happy path filter should still be in effect
        Assert.False(IsVisible(scenarios[1]), "Non-happy path should remain hidden by happy path filter");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Search_NoMatches_AllFeaturesHidden()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [new ScenarioSpec("Making Vanilla Cake")]),
            new FeatureSpec("Drinks", [new ScenarioSpec("Making Lemonade")]));

        SearchFunction.ApplySearch(doc, "chocolate");

        var features = doc.QuerySelectorAll(".feature");
        Assert.False(IsVisible(features[0]));
        Assert.False(IsVisible(features[1]));
    }

    [Fact]
    public void Search_MatchInPlantUml_ScenarioIsVisible()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("API", [
                new ScenarioSpec("Create Order",
                    PlantUml: "@startuml\nOrderService -> PaymentGateway : POST /payments\n@enduml"),
                new ScenarioSpec("Delete Order")
            ]));

        SearchFunction.ApplySearch(doc, "paymentgateway");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]), "Scenario with matching PlantUML should be visible");
        Assert.False(IsVisible(scenarios[1]), "Non-matching scenario should be hidden");
    }

    [Fact]
    public void Search_QuotedPhrase_MatchesExactPhrase()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake"),
                new ScenarioSpec("Chocolate is great, Cake is better")
            ]));

        SearchFunction.ApplySearch(doc, "\"chocolate cake\"");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]), "Exact phrase 'chocolate cake' is in title");
        Assert.False(IsVisible(scenarios[1]), "'chocolate cake' as exact phrase is not in second title");
    }

    [Fact]
    public void Search_MatchInGherkin_NotInTitle_ScenarioIsVisible()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Make Dessert",
                    GherkinSteps: "Given I have chocolate\nAnd I have flour\nWhen I bake\nThen I get cake")
            ]));

        SearchFunction.ApplySearch(doc, "chocolate");

        var scenario = doc.QuerySelector(".scenario")!;
        Assert.True(IsVisible(scenario));
    }

    [Fact]
    public void Search_FeatureWithMixedMatches_OnlyMatchingScenariosShown()
    {
        var doc = BuildStandardReportDom(
            new FeatureSpec("Baking", [
                new ScenarioSpec("Making Chocolate Cake"),
                new ScenarioSpec("Making Vanilla Cake"),
                new ScenarioSpec("Making Chocolate Brownies"),
                new ScenarioSpec("Making Strawberry Tart")
            ]));

        SearchFunction.ApplySearch(doc, "chocolate");

        var scenarios = doc.QuerySelectorAll(".scenario");
        Assert.True(IsVisible(scenarios[0]));  // Chocolate Cake
        Assert.False(IsVisible(scenarios[1])); // Vanilla Cake
        Assert.True(IsVisible(scenarios[2]));  // Chocolate Brownies
        Assert.False(IsVisible(scenarios[3])); // Strawberry Tart
    }

    #endregion
}
