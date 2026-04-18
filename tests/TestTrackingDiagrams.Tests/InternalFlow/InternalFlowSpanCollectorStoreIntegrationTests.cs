using System.Diagnostics;
using TestTrackingDiagrams.InternalFlow;

namespace TestTrackingDiagrams.Tests.InternalFlow;

/// <summary>
/// Tests that <see cref="InternalFlowSpanCollector"/> reads directly from
/// <see cref="InternalFlowSpanStore"/> (no reflection).
/// </summary>
public class InternalFlowSpanCollectorStoreIntegrationTests : IDisposable
{
    private readonly string _sourceName;
    private readonly ActivitySource _source;
    private readonly ActivityListener _rawListener;

    public InternalFlowSpanCollectorStoreIntegrationTests()
    {
        _sourceName = $"CollectorInteg.{Guid.NewGuid():N}";
        _source = new ActivitySource(_sourceName);
        _rawListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == _sourceName || s.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_rawListener);
    }

    public void Dispose()
    {
        _rawListener.Dispose();
        _source.Dispose();
    }

    [Fact]
    public void CollectSpans_returns_spans_from_store()
    {
        using var activity = _source.StartActivity("store-op")!;
        activity.Stop();
        InternalFlowSpanStore.Add(activity);

        var collected = InternalFlowSpanCollector.CollectSpans(InternalFlowSpanGranularity.Full);
        Assert.Contains(collected, s => s.DisplayName == "store-op" && s.Source.Name == _sourceName);
    }

    [Fact]
    public void AutoInstrumentation_includes_custom_spans_sharing_trace_with_well_known()
    {
        using var wellKnown = new ActivitySource("Microsoft.AspNetCore");
        var opName = $"aspnet-{Guid.NewGuid():N}";
        using var a1 = wellKnown.StartActivity(opName)!;
        var wellKnownTraceId = a1.TraceId;
        a1.Stop();
        InternalFlowSpanStore.Add(a1);

        // A custom source span with the SAME TraceId (child of well-known)
        var childContext = new ActivityContext(wellKnownTraceId, a1.SpanId, ActivityTraceFlags.None);
        using var custom = _source.StartActivity("custom-child", ActivityKind.Internal, childContext)!;
        custom.Stop();
        InternalFlowSpanStore.Add(custom);

        // A custom source span with a DIFFERENT TraceId (unrelated)
        var unrelatedOpName = $"unrelated-{Guid.NewGuid():N}";
        using var unrelated = _source.StartActivity(unrelatedOpName)!;
        unrelated.Stop();
        InternalFlowSpanStore.Add(unrelated);

        var collected = InternalFlowSpanCollector.CollectSpans(InternalFlowSpanGranularity.AutoInstrumentation);
        Assert.Contains(collected, s => s.DisplayName == opName);
        Assert.Contains(collected, s => s.DisplayName == "custom-child");
        Assert.DoesNotContain(collected, s => s.DisplayName == unrelatedOpName);
    }
}
