using OpenTelemetry;
using OpenTelemetry.Trace;

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
}
