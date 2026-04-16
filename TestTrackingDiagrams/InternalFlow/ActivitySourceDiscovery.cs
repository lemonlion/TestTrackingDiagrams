namespace TestTrackingDiagrams.InternalFlow;

/// <summary>
/// Discovers and counts Activity sources from spans already stored in
/// <see cref="InternalFlowSpanStore"/>. No separate listener needed.
/// </summary>
public static class ActivitySourceDiscovery
{
    /// <summary>
    /// Returns a dictionary of source name → span count from the current span store.
    /// </summary>
    public static Dictionary<string, int> GetDiscoveredSources()
    {
        var spans = InternalFlowSpanStore.GetSpans();
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var span in spans)
        {
            var name = span.Source.Name;
            if (string.IsNullOrEmpty(name)) continue;

            counts.TryGetValue(name, out var count);
            counts[name] = count + 1;
        }

        return counts;
    }
}
