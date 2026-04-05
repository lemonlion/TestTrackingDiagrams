using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using TestTrackingDiagrams.InternalFlow;

namespace TestTrackingDiagrams.Extensions.OpenTelemetry;

public static class OpenTelemetryTrackingExtensions
{
    /// <summary>
    /// Adds a test-tracking in-memory span exporter to the trace pipeline.
    /// Captured spans are stored in <see cref="TestTrackingSpanStore"/> and used
    /// to generate internal flow diagrams in the HTML report popups.
    /// </summary>
    public static TracerProviderBuilder AddTestTrackingExporter(this TracerProviderBuilder builder)
    {
        return builder.AddProcessor(new SimpleActivityExportProcessor(new TestTrackingSpanExporter()));
    }

    /// <summary>
    /// Configures OpenTelemetry tracing for internal flow tracking.
    /// Registers the test-tracking span exporter, adds all well-known
    /// auto-instrumentation activity sources, and sets <see cref="AlwaysOnSampler"/>
    /// to ensure no spans are dropped.
    /// <para>
    /// Safe to call in <c>ConfigureTestServices</c> whether or not the SUT
    /// already configures OpenTelemetry — the exporter and sources are added
    /// alongside any existing configuration.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection (typically from <c>ConfigureTestServices</c>).</param>
    /// <param name="additionalActivitySources">
    /// Optional custom <see cref="System.Diagnostics.ActivitySource"/> names to
    /// capture in addition to the well-known auto-instrumentation sources.
    /// </param>
    public static IServiceCollection AddInternalFlowTracking(
        this IServiceCollection services,
        params string[] additionalActivitySources)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                foreach (var source in InternalFlowSpanCollector.WellKnownAutoInstrumentationSources)
                    tracing.AddSource(source);

                foreach (var source in additionalActivitySources)
                    tracing.AddSource(source);

                tracing.SetSampler(new AlwaysOnSampler());
                tracing.AddTestTrackingExporter();
            });

        return services;
    }
}
