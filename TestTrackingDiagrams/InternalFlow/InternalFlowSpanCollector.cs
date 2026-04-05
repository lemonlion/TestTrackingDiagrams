using System.Diagnostics;

namespace TestTrackingDiagrams.InternalFlow;

/// <summary>
/// Collects and filters spans based on the configured
/// <see cref="InternalFlowSpanGranularity"/>.
/// </summary>
public static class InternalFlowSpanCollector
{
    public static readonly HashSet<string> WellKnownAutoInstrumentationSources =
    [
        "Microsoft.AspNetCore",
        "System.Net.Http",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "StackExchange.Redis",
        "Azure.Cosmos",
        "Azure.Storage",
        "Microsoft.Azure.Cosmos",
        "OpenTelemetry.Instrumentation.Http",
        "OpenTelemetry.Instrumentation.AspNetCore",
        "OpenTelemetry.Instrumentation.SqlClient",
        "OpenTelemetry.Instrumentation.EntityFrameworkCore"
    ];

    /// <summary>
    /// Collects spans from <see cref="InternalFlowSpanStore"/> and filters
    /// them according to the specified granularity.
    /// </summary>
    public static Activity[] CollectSpans(
        InternalFlowSpanGranularity granularity = InternalFlowSpanGranularity.AutoInstrumentation,
        string[]? manualActivitySources = null)
    {
        var spans = InternalFlowSpanStore.GetSpans();
        return FilterSpans(spans, granularity, manualActivitySources);
    }

    private static Activity[] FilterSpans(
        Activity[] spans,
        InternalFlowSpanGranularity granularity,
        string[]? manualActivitySources)
    {
        return granularity switch
        {
            InternalFlowSpanGranularity.Full => spans,
            InternalFlowSpanGranularity.Manual => FilterByManualSources(spans, manualActivitySources),
            _ => FilterByAutoInstrumentation(spans)
        };
    }

    private static Activity[] FilterByAutoInstrumentation(Activity[] spans)
    {
        return spans
            .Where(s => !string.IsNullOrEmpty(s.Source.Name) &&
                        WellKnownAutoInstrumentationSources.Contains(s.Source.Name))
            .ToArray();
    }

    private static Activity[] FilterByManualSources(Activity[] spans, string[]? sources)
    {
        if (sources is null or { Length: 0 })
            return spans;

        var sourceSet = new HashSet<string>(sources, StringComparer.OrdinalIgnoreCase);
        return spans
            .Where(s => !string.IsNullOrEmpty(s.Source.Name) &&
                        sourceSet.Contains(s.Source.Name))
            .ToArray();
    }
}
