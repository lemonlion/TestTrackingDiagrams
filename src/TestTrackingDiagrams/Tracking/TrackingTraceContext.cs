using System.Diagnostics;

namespace TestTrackingDiagrams.Tracking;

public static class TrackingTraceContext
{
    private static readonly AsyncLocal<Guid?> Current = new();

    public static Guid? CurrentTraceId => Current.Value;

    public static IDisposable BeginTrace()
    {
        return BeginTrace(out _);
    }

    public static IDisposable BeginTrace(out Guid traceId)
    {
        var previous = Current.Value;
        traceId = Guid.NewGuid();
        Current.Value = traceId;
        return new TraceScope(previous);
    }

    public static ActivityContext CreateParentContext()
    {
        var traceId = Current.Value;
        if (traceId is null) return default;

        var bytes = traceId.Value.ToByteArray();
        var activityTraceId = ActivityTraceId.CreateFromBytes(bytes);
        var activitySpanId = ActivitySpanId.CreateRandom();
        return new ActivityContext(activityTraceId, activitySpanId, ActivityTraceFlags.Recorded);
    }

    private sealed class TraceScope(Guid? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}
