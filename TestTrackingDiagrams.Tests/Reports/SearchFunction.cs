using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// C# port of the JavaScript search logic used in both the standard FeaturesReport
/// and the LightBDD report. This class exists so the search behaviour can be
/// thoroughly tested via unit tests. The actual JS in the reports must implement
/// identical logic.
/// </summary>
public static class SearchFunction
{
    private const string SearchHiddenClass = "search-hidden";

    /// <summary>
    /// Parses a search input string into tokens. Quoted phrases are kept as single
    /// tokens; unquoted words are split on whitespace. All tokens are lowercased.
    /// </summary>
    public static string[] ParseSearchTokens(string input)
    {
        input = input.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(input))
            return [];

        var tokens = new List<string>();

        // Extract quoted phrases first
        var quoteRegex = new Regex("\"(.*?)\"");
        var matches = quoteRegex.Matches(input);
        foreach (Match match in matches)
        {
            var phrase = match.Groups[1].Value.Trim();
            if (phrase.Length > 0)
                tokens.Add(phrase);
        }

        // Remove quoted phrases from the input
        var remaining = quoteRegex.Replace(input, "").Trim();

        // Split remaining on whitespace
        if (remaining.Length > 0)
        {
            var words = remaining.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                var trimmed = word.Trim();
                if (trimmed.Length > 0)
                    tokens.Add(trimmed);
            }
        }

        return tokens.ToArray();
    }

    /// <summary>
    /// Returns true if the scenario element's text content (title, description,
    /// gherkin steps, plantuml code) contains ALL of the given tokens (case-insensitive).
    /// </summary>
    public static bool ScenarioMatchesAllTokens(IElement scenarioElement, string[] tokens)
    {
        var text = scenarioElement.TextContent.ToLowerInvariant();
        return tokens.All(token => text.Contains(token));
    }

    /// <summary>
    /// Applies search filtering to the document. This is the main entry point that
    /// mirrors the JS run_search_scenarios function.
    /// 
    /// - Empty/whitespace input: clears search filtering (restores all search-hidden elements)
    /// - Single match: shows scenario expanded with diagrams open but raw plantuml closed
    /// - Multiple matches: shows matching scenarios retracted
    /// - Non-matching scenarios get search-hidden class
    /// - Features with no visible scenarios get search-hidden class
    /// - Must not interfere with .hide class (used by happy path / LightBDD filters)
    /// </summary>
    public static void ApplySearch(IDocument document, string searchInput)
    {
        var tokens = ParseSearchTokens(searchInput);

        // Clear all previous search state
        foreach (var el in document.QuerySelectorAll("." + SearchHiddenClass))
            el.ClassList.Remove(SearchHiddenClass);

        var allScenarios = document.QuerySelectorAll(".scenario");
        var allFeatures = document.QuerySelectorAll(".feature");

        // Reset scenario open/closed state that search may have set
        foreach (var scenario in allScenarios)
            scenario.RemoveAttribute("open");

        // If search is empty, just clear and return (other filters remain)
        if (tokens.Length == 0)
        {
            // Reset feature opacity
            foreach (var feature in allFeatures)
                feature.RemoveAttribute("style");
            return;
        }

        // Find matching scenarios
        var matchingScenarios = new List<IElement>();
        foreach (var scenario in allScenarios)
        {
            if (ScenarioMatchesAllTokens(scenario, tokens))
            {
                matchingScenarios.Add(scenario);
            }
            else
            {
                scenario.ClassList.Add(SearchHiddenClass);
            }
        }

        // Handle expand/retract based on match count
        if (matchingScenarios.Count == 1)
        {
            // Single match: expand the scenario
            var scenario = matchingScenarios[0];
            scenario.SetAttribute("open", "");

            // Open diagram details but not the inner raw plantuml
            var diagramDetails = scenario.QuerySelector("details.example-diagrams");
            diagramDetails?.SetAttribute("open", "");

            // Ensure raw plantuml (details.example) is closed
            var exampleDetails = scenario.QuerySelector("details.example");
            exampleDetails?.RemoveAttribute("open");
        }
        // Multiple matches: leave retracted (default state, no 'open' attribute)

        // Hide features that have no visible scenarios
        foreach (var feature in allFeatures)
        {
            var featureScenarios = feature.QuerySelectorAll(".scenario");
            var hasVisible = featureScenarios.Any(s => !s.ClassList.Contains(SearchHiddenClass));

            if (!hasVisible)
            {
                feature.ClassList.Add(SearchHiddenClass);
            }
        }

        // Clear opacity
        foreach (var feature in allFeatures)
        {
            var style = feature.GetAttribute("style") ?? "";
            if (style.Contains("opacity"))
                feature.RemoveAttribute("style");
        }
    }
}
