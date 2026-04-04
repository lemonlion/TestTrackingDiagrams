using System.Collections.Concurrent;
using System.Diagnostics;

namespace TestTrackingDiagrams.Extensions.OpenTelemetry;

public static class TestTrackingSpanStore
{
    private static readonly ConcurrentQueue<Activity> CollectedSpans = new();

    internal static void Add(Activity activity) => CollectedSpans.Enqueue(activity);

    public static Activity[] GetSpans() => CollectedSpans.ToArray();

    public static void Clear() => CollectedSpans.Clear();
}
