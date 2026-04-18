using System.Diagnostics;
using TestTrackingDiagrams.InternalFlow;

namespace TestTrackingDiagrams.Extensions.OpenTelemetry;

/// <summary>
/// Backward-compatible facade that delegates to <see cref="InternalFlowSpanStore"/>
/// in the core package. Existing code referencing this class continues to work.
/// </summary>
public static class TestTrackingSpanStore
{
    public static void Add(Activity activity) => InternalFlowSpanStore.Add(activity);

    public static Activity[] GetSpans() => InternalFlowSpanStore.GetSpans();

    public static void Clear() => InternalFlowSpanStore.Clear();
}
