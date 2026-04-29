using OpenTelemetry;
using OpenTelemetry.Trace;

namespace TestTrackingDiagrams.Extensions.OpenTelemetry;

/// <summary>
/// Provides extension methods for enabling OpenTelemetry integration with test tracking.
/// </summary>
public static class OpenTelemetryTrackingExtensions
{
    /// <summary>
    /// Adds a test-tracking in-memory span exporter to the trace pipeline.
    /// Captured spans are stored in <see cref="InternalFlow.InternalFlowSpanStore"/> and used
    /// to generate internal flow diagrams in the HTML report popups.
    /// <para>
    /// This is the manual OTel SDK integration point. For a non-invasive
    /// one-liner that works without configuring OpenTelemetry, use
    /// <c>services.AddOpenTelemetryForInternalFlowTracking()</c> from the
    /// core <c>TestTrackingDiagrams</c> package instead.
    /// </para>
    /// </summary>
    public static TracerProviderBuilder AddTestTrackingExporter(this TracerProviderBuilder builder)
    {
        return builder.AddProcessor(new SimpleActivityExportProcessor(new TestTrackingSpanExporter()));
    }
}