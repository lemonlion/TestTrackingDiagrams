using System.Diagnostics;
using TestTrackingDiagrams.InternalFlow;

namespace TestTrackingDiagrams.Tests.InternalFlow;

/// <summary>
/// Tests for <see cref="InternalFlowActivityListener"/> — the non-invasive
/// BCL ActivityListener that captures spans without touching the OTel SDK.
/// All assertions filter by unique source name for parallel safety.
/// </summary>
public class InternalFlowActivityListenerTests : IDisposable
{
    private InternalFlowActivityListener? _listener;

    public void Dispose()
    {
        _listener?.Dispose();
    }

    [Fact]
    public void Captures_spans_from_well_known_auto_instrumentation_source()
    {
        _listener = new InternalFlowActivityListener();

        // Use a unique-ish operation name to avoid collision
        var opName = $"GET-{Guid.NewGuid():N}";
        using var source = new ActivitySource("System.Net.Http");
        using var activity = source.StartActivity(opName);
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(), s => s.DisplayName == opName);
    }

    [Fact]
    public void Captures_spans_from_additional_custom_source()
    {
        var sourceName = $"Custom.{Guid.NewGuid():N}";
        _listener = new InternalFlowActivityListener(sourceName);

        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("custom-operation");
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "custom-operation" && s.Source.Name == sourceName);
    }

    [Fact]
    public void Does_not_capture_unregistered_source()
    {
        var sourceName = $"Unregistered.{Guid.NewGuid():N}";
        _listener = new InternalFlowActivityListener();

        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("unregistered-op");
        activity?.Stop();

        Assert.DoesNotContain(InternalFlowSpanStore.GetSpans(),
            s => s.Source.Name == sourceName);
    }

    [Fact]
    public void Uses_AllData_sampling_not_AllDataAndRecorded()
    {
        var sourceName = $"SamplingCheck.{Guid.NewGuid():N}";
        _listener = new InternalFlowActivityListener(sourceName);

        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("sampled-op");
        Assert.NotNull(activity);
        Assert.True(activity!.IsAllDataRequested);

        activity.Stop();
        Assert.Contains(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "sampled-op" && s.Source.Name == sourceName);
    }

    [Fact]
    public void Captures_full_activity_properties_for_rendering()
    {
        var sourceName = $"FullData.{Guid.NewGuid():N}";
        _listener = new InternalFlowActivityListener(sourceName);

        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("data-op");
        Assert.NotNull(activity);
        activity!.Stop();

        var span = InternalFlowSpanStore.GetSpans().Single(s => s.Source.Name == sourceName);
        Assert.Equal("data-op", span.DisplayName);
        Assert.Equal(sourceName, span.Source.Name);
        Assert.NotEqual(default, span.Duration);
    }

    [Fact]
    public void Works_with_multiple_additional_sources()
    {
        var name1 = $"Multi.A.{Guid.NewGuid():N}";
        var name2 = $"Multi.B.{Guid.NewGuid():N}";
        _listener = new InternalFlowActivityListener(name1, name2);

        using var source1 = new ActivitySource(name1);
        using var source2 = new ActivitySource(name2);
        using var a1 = source1.StartActivity("op-a");
        a1?.Stop();
        using var a2 = source2.StartActivity("op-b");
        a2?.Stop();

        var spans = InternalFlowSpanStore.GetSpans();
        Assert.Contains(spans, s => s.DisplayName == "op-a" && s.Source.Name == name1);
        Assert.Contains(spans, s => s.DisplayName == "op-b" && s.Source.Name == name2);
    }

    [Fact]
    public void Works_with_no_additional_sources()
    {
        _listener = new InternalFlowActivityListener();

        var opName = $"aspnet-{Guid.NewGuid():N}";
        using var source = new ActivitySource("Microsoft.AspNetCore");
        using var activity = source.StartActivity(opName);
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(), s => s.DisplayName == opName);
    }

    [Fact]
    public void Dispose_stops_capturing()
    {
        var sourceName = $"Dispose.{Guid.NewGuid():N}";
        _listener = new InternalFlowActivityListener(sourceName);

        using var source = new ActivitySource(sourceName);

        using (var a1 = source.StartActivity("before-dispose"))
            a1?.Stop();
        Assert.Contains(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "before-dispose" && s.Source.Name == sourceName);

        _listener.Dispose();
        _listener = null;

        // After dispose, new activities from this source should not be captured
        // (unless another listener in the process also subscribes — but since
        // the source name is unique, only our listener was subscribed)
        using (var a2 = source.StartActivity("after-dispose"))
            a2?.Stop(); // activity may be null since no listener subscribes

        Assert.DoesNotContain(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "after-dispose" && s.Source.Name == sourceName);
    }

    [Fact]
    public void All_spans_captured_none_dropped_by_sampling()
    {
        var sourceName = $"Sampler.{Guid.NewGuid():N}";
        _listener = new InternalFlowActivityListener(sourceName);

        using var source = new ActivitySource(sourceName);

        const int spanCount = 50;
        for (var i = 0; i < spanCount; i++)
        {
            using var activity = source.StartActivity($"op-{i}");
            activity?.Stop();
        }

        Assert.Equal(spanCount, InternalFlowSpanStore.GetSpans().Count(s => s.Source.Name == sourceName));
    }

    [Fact]
    public void Preserves_parent_child_span_hierarchy()
    {
        var sourceName = $"Hierarchy.{Guid.NewGuid():N}";
        _listener = new InternalFlowActivityListener(sourceName);

        using var source = new ActivitySource(sourceName);

        using var parent = source.StartActivity("parent-op");
        Assert.NotNull(parent);

        using var child = source.StartActivity("child-op");
        Assert.NotNull(child);
        Assert.Equal(parent!.SpanId, child!.ParentSpanId);

        child.Stop();
        parent.Stop();
    }
}
