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
    public void AutoInstrumentation_filters_to_well_known_sources()
    {
        using var wellKnown = new ActivitySource("Microsoft.AspNetCore");
        var opName = $"aspnet-{Guid.NewGuid():N}";
        using var a1 = wellKnown.StartActivity(opName)!;
        a1.Stop();
        InternalFlowSpanStore.Add(a1);

        using var custom = _source.StartActivity("custom-op")!;
        custom.Stop();
        InternalFlowSpanStore.Add(custom);

        var collected = InternalFlowSpanCollector.CollectSpans(InternalFlowSpanGranularity.AutoInstrumentation);
        Assert.Contains(collected, s => s.DisplayName == opName);
        Assert.DoesNotContain(collected, s => s.Source.Name == _sourceName);
    }
}
