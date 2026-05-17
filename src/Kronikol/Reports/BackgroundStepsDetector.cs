namespace Kronikol.Reports;

/// <summary>
/// Detects common step prefixes across scenarios (heuristic for Gherkin Background sections)
/// and extracts them into <see cref="Scenario.BackgroundSteps"/>.
/// Groups scenarios by <see cref="Scenario.Rule"/> so each Rule can have its own background.
/// </summary>
public static class BackgroundStepsDetector
{
    /// <summary>
    /// Detects and extracts common step prefixes within each Rule group.
    /// Mutates the scenarios in-place: sets <see cref="Scenario.BackgroundSteps"/>
    /// and trims the prefix from <see cref="Scenario.Steps"/>.
    /// </summary>
    public static void DetectAndExtract(Scenario[] scenarios)
    {
        if (scenarios.Length < 2)
            return;

        var groups = scenarios.GroupBy(s => s.Rule);

        foreach (var group in groups)
        {
            var members = group.Where(s => s.Steps is { Length: > 0 }).ToArray();
            if (members.Length < 2)
                continue;

            // If any scenario starts with "And" or "When", the shared prefix is not
            // a Gherkin Background – skip extraction for this group.
            if (members.Any(s => s.Steps![0].Keyword is "And" or "When"))
                continue;

            var commonPrefixLength = FindCommonPrefixLength(members);
            if (commonPrefixLength == 0)
                continue;

            // Don't extract if any scenario's first remaining step starts with "Given" or "When";
            // background extraction only makes sense when remaining steps start with continuation
            // keywords (Then, And, But) or when all steps are common (no remaining steps).
            if (members.Any(s => s.Steps!.Length > commonPrefixLength &&
                                 s.Steps![commonPrefixLength].Keyword is "Given" or "When"))
                continue;

            // Use the first scenario's steps as the background template
            var backgroundSteps = members[0].Steps!.Take(commonPrefixLength).ToArray();

            foreach (var scenario in members)
            {
                scenario.BackgroundSteps = backgroundSteps;
                scenario.Steps = scenario.Steps!.Skip(commonPrefixLength).ToArray();
            }
        }
    }

    private static int FindCommonPrefixLength(Scenario[] scenarios)
    {
        var minStepCount = scenarios.Min(s => s.Steps!.Length);
        var prefixLength = 0;

        for (var i = 0; i < minStepCount; i++)
        {
            var reference = scenarios[0].Steps![i];
            var allMatch = scenarios.All(s =>
                s.Steps![i].Keyword == reference.Keyword &&
                s.Steps[i].Text == reference.Text);

            if (!allMatch)
                break;

            prefixLength++;
        }

        return prefixLength;
    }
}
