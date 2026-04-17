namespace TestTrackingDiagrams.Reports;

public static class ParameterGrouper
{
    /// <summary>
    /// Analyzes scenarios and groups parameterized test runs sharing the same test method.
    /// Returns (parameterizedGroups, ungroupedScenarios).
    /// </summary>
    public static (ParameterizedGroup[] Groups, Scenario[] Ungrouped) Analyze(
        Scenario[] scenarios,
        bool enabled = true,
        int maxColumns = 10,
        Func<Scenario[], bool>? diagramComparer = null)
    {
        if (scenarios.Length == 0)
            return ([], scenarios);

        var groups = new List<ParameterizedGroup>();
        var ungrouped = new List<Scenario>();
        var consumed = new HashSet<string>(); // scenario IDs already assigned to a group

        // 1. Group by OutlineId (framework-provided, e.g. ReqNRoll) — always active
        var outlineGroups = scenarios
            .Where(s => s.OutlineId is not null)
            .GroupBy(s => s.OutlineId!)
            .ToArray();

        foreach (var og in outlineGroups)
        {
            var members = og.ToArray();
            if (members.Length < 2)
            {
                ungrouped.AddRange(members);
            }
            else
            {
                groups.Add(BuildGroup(og.Key, members, maxColumns, diagramComparer));
            }
            foreach (var m in members) consumed.Add(m.Id);
        }

        // 2. Group remaining scenarios by display name prefix (only when enabled)
        var remaining = scenarios.Where(s => !consumed.Contains(s.Id)).ToArray();
        if (!enabled)
            return (groups.ToArray(), remaining);

        var prefixGroups = remaining
            .Select(s => (Scenario: s, BaseName: ParameterParser.ExtractBaseName(s.DisplayName) ?? s.DisplayName))
            .GroupBy(x => x.BaseName)
            .ToArray();

        foreach (var pg in prefixGroups)
        {
            var members = pg.Select(x => x.Scenario).ToArray();
            if (members.Length < 2)
            {
                ungrouped.AddRange(members);
            }
            else
            {
                groups.Add(BuildGroup(pg.Key, members, maxColumns, diagramComparer));
            }
        }

        return (groups.ToArray(), ungrouped.ToArray());
    }

    private static ParameterizedGroup BuildGroup(
        string groupName,
        Scenario[] members,
        int maxColumns,
        Func<Scenario[], bool>? diagramComparer)
    {
        // Determine parameter names and rule
        var (paramNames, rule) = DetermineParamsAndRule(members, maxColumns);
        var identical = diagramComparer?.Invoke(members) ?? false;

        return new ParameterizedGroup(groupName, paramNames, rule, members, identical);
    }

    private static (string[] ParamNames, ParameterDisplayRule Rule) DetermineParamsAndRule(
        Scenario[] members, int maxColumns)
    {
        // If any member has an ExampleDisplayName (custom display name), use Fallback
        if (members.Any(m => m.ExampleDisplayName is not null))
            return ([], ParameterDisplayRule.Fallback);

        // Try ExampleValues first (structured params from framework)
        var withExampleValues = members.Where(m => m.ExampleValues is { Count: > 0 }).ToArray();
        if (withExampleValues.Length == members.Length)
        {
            var keys = withExampleValues
                .SelectMany(m => m.ExampleValues!.Keys)
                .Distinct()
                .ToArray();

            if (keys.Length > maxColumns)
                return ([], ParameterDisplayRule.Fallback);

            return (keys, ParameterDisplayRule.ScalarColumns);
        }

        // Fall back to parsing display names
        var allParsed = new List<Dictionary<string, string>>();
        foreach (var m in members)
        {
            var parsed = ParameterParser.Parse(m.DisplayName);
            if (parsed is null || parsed.Count == 0)
                return ([], ParameterDisplayRule.Fallback);
            allParsed.Add(parsed);

            // Populate ExampleValues on the scenario so the renderer can use them
            m.ExampleValues ??= parsed;
        }

        var allKeys = allParsed.SelectMany(d => d.Keys).Distinct().ToArray();
        if (allKeys.Length > maxColumns)
            return ([], ParameterDisplayRule.Fallback);

        return (allKeys, ParameterDisplayRule.ScalarColumns);
    }
}
