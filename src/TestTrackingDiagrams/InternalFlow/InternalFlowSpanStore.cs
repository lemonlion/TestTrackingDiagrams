using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TestTrackingDiagrams.InternalFlow;

/// <summary>
/// Thread-safe in-process store for captured <see cref="Activity"/> spans.
/// Both the <see cref="InternalFlowActivityListener"/> (BCL path) and
/// the OTel SDK exporter (<c>TestTrackingSpanExporter</c>) write here.
/// <see cref="InternalFlowSpanCollector"/> reads from here at report generation time.
/// Deduplicates by reference identity — safe when both listener and exporter capture the same Activity.
/// </summary>
public static class InternalFlowSpanStore
{
    private static readonly ConcurrentQueue<Activity> CollectedSpans = new();
    private static readonly ConcurrentDictionary<Activity, byte> SeenSpans =
        new(ReferenceEqualityComparer.Instance);

    public static void Add(Activity activity)
    {
        if (SeenSpans.TryAdd(activity, 0))
            CollectedSpans.Enqueue(activity);
    }

    public static Activity[] GetSpans() => CollectedSpans.ToArray();

    /// <summary>
    /// Stops the activity (if still running) and adds it to the store.
    /// Useful as a one-liner at the end of an in-process tracking scope.
    /// </summary>
    public static void Complete(Activity? activity)
    {
        if (activity is null) return;
        if (!activity.IsStopped) activity.Stop();
        Add(activity);
    }

    public static void Clear()
    {
        SeenSpans.Clear();
        CollectedSpans.Clear();
    }
}
