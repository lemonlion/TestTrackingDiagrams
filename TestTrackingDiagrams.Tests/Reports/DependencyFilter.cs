using AngleSharp.Dom;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// C# port of the JavaScript dependency filter logic used in the FeaturesReport.
/// Uses 'dep-hidden' class (analogous to 'search-hidden') so dependency filtering
/// and search filtering operate independently and compose via CSS.
/// </summary>
public static class DependencyFilter
{
    private const string DepHiddenClass = "dep-hidden";

    /// <summary>
    /// Applies dependency filtering to the document.
    /// - Empty activeDeps: clears dep filtering (all dep-hidden removed)
    /// - Non-empty activeDeps: hides scenarios whose data-dependencies
    ///   attribute does not contain ALL of the active dependencies
    /// </summary>
    public static void Apply(IDocument document, string[] activeDeps)
    {
        // Clear previous dep filtering
        foreach (var el in document.QuerySelectorAll("." + DepHiddenClass))
            el.ClassList.Remove(DepHiddenClass);

        // Clear feature-level dep state
        var allFeatures = document.QuerySelectorAll(".feature");
        foreach (var feature in allFeatures)
        {
            if (feature.ClassList.Contains("dep-opened"))
            {
                feature.RemoveAttribute("open");
                feature.ClassList.Remove("dep-opened");
            }
        }

        if (activeDeps.Length == 0)
            return;

        var allScenarios = document.QuerySelectorAll(".scenario");
        var totalVisible = 0;

        foreach (var scenario in allScenarios)
        {
            var depsAttr = scenario.GetAttribute("data-dependencies") ?? "";
            var scenarioDeps = depsAttr.Length > 0
                ? new HashSet<string>(depsAttr.Split(','))
                : new HashSet<string>();

            var matchesAll = activeDeps.All(d => scenarioDeps.Contains(d));
            if (!matchesAll)
            {
                scenario.ClassList.Add(DepHiddenClass);
            }
            else if (!scenario.ClassList.Contains("search-hidden") &&
                     !scenario.ClassList.Contains("status-hidden") &&
                     !scenario.ClassList.Contains("hp-hidden"))
            {
                totalVisible++;
            }
        }

        var shouldOpen = totalVisible <= 10;

        // Hide features with no visible scenarios, open features with matches
        foreach (var feature in allFeatures)
        {
            var featureScenarios = feature.QuerySelectorAll(".scenario");
            var hasVisible = featureScenarios.Any(s =>
                !s.ClassList.Contains(DepHiddenClass) &&
                !s.ClassList.Contains("search-hidden"));

            if (!hasVisible)
            {
                feature.ClassList.Add(DepHiddenClass);
            }
            else if (shouldOpen && !feature.HasAttribute("open"))
            {
                feature.SetAttribute("open", "");
                feature.ClassList.Add("dep-opened");
            }
        }
    }
}
