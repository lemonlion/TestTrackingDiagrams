using System.Diagnostics;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

/// <summary>
/// Tests for auto-starting <see cref="InternalFlowActivityListener"/>
/// from the <see cref="TestTrackingMessageHandler"/> constructor.
/// All tests use well-known sources + unique operation names for parallel safety.
/// No <c>ResetForTesting</c> — the static singleton is process-wide and shared.
/// </summary>
[Collection("InternalFlowSpanStore")]
public class AutoStartActivityListenerTests
{
    [Fact]
    public void Handler_construction_auto_starts_listener_for_custom_sources()
    {
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());

        var sourceName = $"CustomApp.{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("custom-op");
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "custom-op" && s.Source.Name == sourceName);
    }

    [Fact]
    public void Handler_construction_does_not_listen_to_well_known_sources()
    {
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());

        var opName = $"http-{Guid.NewGuid():N}";
        using var source = new ActivitySource("System.Net.Http");
        using var activity = source.StartActivity(opName);
        activity?.Stop();

        Assert.DoesNotContain(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == opName && s.Source.Name == "System.Net.Http");
    }

    [Fact]
    public void Multiple_handler_constructions_do_not_duplicate_spans()
    {
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());

        var sourceName = $"MultiHandler.{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("once-op");
        activity?.Stop();

        Assert.Single(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "once-op" && s.Source.Name == sourceName);
    }

    [Fact]
    public void Auto_started_listener_captures_any_source()
    {
        var unknownSource = $"Unknown.{Guid.NewGuid():N}";
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());

        using var source = new ActivitySource(unknownSource);
        using var activity = source.StartActivity("unknown-op");
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(),
            s => s.Source.Name == unknownSource);
    }

    [Fact]
    public void Auto_started_listener_captures_spans_on_activity_stop()
    {
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());

        var sourceName = $"StopTest.{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("stop-op");
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "stop-op" && s.Source.Name == sourceName);
    }

    [Fact]
    public void InternalFlowActivitySources_option_is_available_on_handler_options()
    {
        var customSources = new[] { "MyApp.Services", "MyApp.Database" };
        var options = new TestTrackingMessageHandlerOptions
        {
            InternalFlowActivitySources = customSources
        };

        Assert.Equal(customSources, options.InternalFlowActivitySources);
    }
}
