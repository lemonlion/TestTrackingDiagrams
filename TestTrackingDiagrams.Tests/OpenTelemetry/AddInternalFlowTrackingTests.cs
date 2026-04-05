using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using TestTrackingDiagrams.Extensions.OpenTelemetry;

namespace TestTrackingDiagrams.Tests.OpenTelemetry;

public class AddInternalFlowTrackingTests : IDisposable
{
    private ServiceProvider? _provider;

    public void Dispose()
    {
        _provider?.Dispose();
        TestTrackingSpanStore.Clear();
    }

    private ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        _provider = services.BuildServiceProvider();

        // Force TracerProvider initialisation
        _provider.GetService<TracerProvider>();

        return _provider;
    }

    [Fact]
    public void Captures_spans_from_well_known_auto_instrumentation_source()
    {
        // Use a well-known source that won't collide with real infra
        using var source = new ActivitySource("System.Net.Http");
        BuildProvider(s => s.AddInternalFlowTracking());

        using var activity = source.StartActivity("GET /api/test");
        activity?.Stop();

        var spans = TestTrackingSpanStore.GetSpans();
        Assert.Contains(spans, s => s.DisplayName == "GET /api/test");
    }

    [Fact]
    public void Captures_spans_from_additional_custom_source()
    {
        var sourceName = $"Custom.Source.{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        BuildProvider(s => s.AddInternalFlowTracking(sourceName));

        using var activity = source.StartActivity("custom-operation");
        activity?.Stop();

        var spans = TestTrackingSpanStore.GetSpans();
        Assert.Contains(spans, s => s.DisplayName == "custom-operation");
    }

    [Fact]
    public void Does_not_capture_unregistered_source()
    {
        var sourceName = $"Unregistered.Source.{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        BuildProvider(s => s.AddInternalFlowTracking());

        using var activity = source.StartActivity("unregistered-op");
        activity?.Stop();

        var spans = TestTrackingSpanStore.GetSpans();
        Assert.DoesNotContain(spans, s => s.DisplayName == "unregistered-op");
    }

    [Fact]
    public void Sampler_is_always_on_so_no_spans_are_dropped()
    {
        var sourceName = $"Sampler.Test.{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        BuildProvider(s => s.AddInternalFlowTracking(sourceName));

        const int spanCount = 50;
        for (var i = 0; i < spanCount; i++)
        {
            using var activity = source.StartActivity($"op-{i}");
            activity?.Stop();
        }

        var spans = TestTrackingSpanStore.GetSpans();
        Assert.Equal(spanCount, spans.Count(s => s.Source.Name == sourceName));
    }

    [Fact]
    public void Returns_service_collection_for_fluent_chaining()
    {
        var services = new ServiceCollection();

        var result = services.AddInternalFlowTracking();

        Assert.Same(services, result);
    }

    [Fact]
    public void Works_with_multiple_additional_sources()
    {
        var sourceName1 = $"Multi.A.{Guid.NewGuid():N}";
        var sourceName2 = $"Multi.B.{Guid.NewGuid():N}";
        using var source1 = new ActivitySource(sourceName1);
        using var source2 = new ActivitySource(sourceName2);

        BuildProvider(s => s.AddInternalFlowTracking(sourceName1, sourceName2));

        using var a1 = source1.StartActivity("op-a");
        a1?.Stop();
        using var a2 = source2.StartActivity("op-b");
        a2?.Stop();

        var spans = TestTrackingSpanStore.GetSpans();
        Assert.Contains(spans, s => s.DisplayName == "op-a");
        Assert.Contains(spans, s => s.DisplayName == "op-b");
    }

    [Fact]
    public void Works_with_no_additional_sources()
    {
        // Should not throw when called with zero params
        BuildProvider(s => s.AddInternalFlowTracking());

        // Provider is built and functional
        Assert.NotNull(_provider!.GetService<TracerProvider>());
    }

    [Fact]
    public void Safe_to_call_alongside_existing_otel_registration()
    {
        var existingSourceName = $"Existing.Source.{Guid.NewGuid():N}";
        var additionalSourceName = $"Additional.Source.{Guid.NewGuid():N}";
        using var existingSource = new ActivitySource(existingSourceName);
        using var additionalSource = new ActivitySource(additionalSourceName);

        BuildProvider(s =>
        {
            // Simulate SUT's existing OTel configuration
            s.AddOpenTelemetry()
                .WithTracing(b => b.AddSource(existingSourceName));

            // Then test-time override adds internal flow tracking
            s.AddInternalFlowTracking(additionalSourceName);
        });

        using var a1 = existingSource.StartActivity("existing-op");
        a1?.Stop();
        using var a2 = additionalSource.StartActivity("additional-op");
        a2?.Stop();

        var spans = TestTrackingSpanStore.GetSpans();
        // Both sources should be captured
        Assert.Contains(spans, s => s.DisplayName == "existing-op");
        Assert.Contains(spans, s => s.DisplayName == "additional-op");
    }
}
