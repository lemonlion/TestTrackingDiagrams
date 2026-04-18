using AngleSharp.Dom;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// C# port of the JavaScript status filter logic used in the FeaturesReport.
/// Uses 'status-hidden' class so it composes independently with search-hidden and dep-hidden.
/// </summary>
public static class StatusFilter
{
    private const string StatusHiddenClass = "status-hidden";

    /// <summary>
    /// Applies status filtering to the document.
    /// - Empty activeStatuses: clears status filtering
    /// - Non-empty activeStatuses: hides scenarios whose data-status
    ///   attribute is not in the active set
    /// </summary>
    public static void Apply(IDocument document, string[] activeStatuses)
    {
        foreach (var el in document.QuerySelectorAll("." + StatusHiddenClass))
            el.ClassList.Remove(StatusHiddenClass);

        var allFeatures = document.QuerySelectorAll(".feature");
        foreach (var feature in allFeatures)
        {
            if (feature.ClassList.Contains("status-opened"))
            {
                feature.RemoveAttribute("open");
                feature.ClassList.Remove("status-opened");
            }
        }

        if (activeStatuses.Length == 0)
            return;

        var activeSet = new HashSet<string>(activeStatuses, StringComparer.OrdinalIgnoreCase);
        var totalVisible = 0;

        foreach (var scenario in document.QuerySelectorAll(".scenario"))
        {
            var status = scenario.GetAttribute("data-status") ?? "";
            if (!activeSet.Contains(status))
            {
                scenario.ClassList.Add(StatusHiddenClass);
            }
            else if (!scenario.ClassList.Contains("dep-hidden") &&
                     !scenario.ClassList.Contains("search-hidden") &&
                     !scenario.ClassList.Contains("hp-hidden"))
            {
                totalVisible++;
            }
        }

        var shouldOpen = totalVisible <= 10;

        foreach (var feature in allFeatures)
        {
            var featureScenarios = feature.QuerySelectorAll(".scenario");
            var hasVisible = featureScenarios.Any(s =>
                !s.ClassList.Contains(StatusHiddenClass) &&
                !s.ClassList.Contains("dep-hidden") &&
                !s.ClassList.Contains("search-hidden"));

            if (!hasVisible)
            {
                feature.ClassList.Add(StatusHiddenClass);
            }
            else if (shouldOpen && !feature.HasAttribute("open"))
            {
                feature.SetAttribute("open", "");
                feature.ClassList.Add("status-opened");
            }
        }
    }
}
