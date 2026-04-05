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
public class AutoStartActivityListenerTests
{
    [Fact]
    public void Handler_construction_auto_starts_listener_for_well_known_sources()
    {
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());

        var opName = $"aspnet-{Guid.NewGuid():N}";
        using var source = new ActivitySource("Microsoft.AspNetCore");
        using var activity = source.StartActivity(opName);
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(), s => s.DisplayName == opName);
    }

    [Fact]
    public void Handler_construction_auto_starts_for_http_client_source()
    {
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());

        var opName = $"http-{Guid.NewGuid():N}";
        using var source = new ActivitySource("System.Net.Http");
        using var activity = source.StartActivity(opName);
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(), s => s.DisplayName == opName);
    }

    [Fact]
    public void Multiple_handler_constructions_do_not_duplicate_spans()
    {
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());
        _ = new TestTrackingMessageHandler(new TestTrackingMessageHandlerOptions());

        var opName = $"once-{Guid.NewGuid():N}";
        using var source = new ActivitySource("System.Net.Http");
        using var activity = source.StartActivity(opName);
        activity?.Stop();

        Assert.Single(InternalFlowSpanStore.GetSpans(), s => s.DisplayName == opName);
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

        var opName = $"ef-{Guid.NewGuid():N}";
        using var source = new ActivitySource("Microsoft.EntityFrameworkCore");
        using var activity = source.StartActivity(opName);
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(), s => s.DisplayName == opName);
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
