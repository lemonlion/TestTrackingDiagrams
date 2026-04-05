using System.Collections.Concurrent;
using System.Diagnostics;

namespace TestTrackingDiagrams.InternalFlow;

/// <summary>
/// Thread-safe in-process store for captured <see cref="Activity"/> spans.
/// Both the <see cref="InternalFlowActivityListener"/> (BCL path) and
/// the OTel SDK exporter (<c>TestTrackingSpanExporter</c>) write here.
/// <see cref="InternalFlowSpanCollector"/> reads from here at report generation time.
/// </summary>
public static class InternalFlowSpanStore
{
    private static readonly ConcurrentQueue<Activity> CollectedSpans = new();

    public static void Add(Activity activity) => CollectedSpans.Enqueue(activity);

    public static Activity[] GetSpans() => CollectedSpans.ToArray();

    public static void Clear() => CollectedSpans.Clear();
}
