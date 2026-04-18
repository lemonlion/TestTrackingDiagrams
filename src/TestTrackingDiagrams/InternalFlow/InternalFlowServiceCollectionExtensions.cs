using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.InternalFlow;

public static class InternalFlowServiceCollectionExtensions
{
    /// <summary>
    /// Registers a non-invasive <see cref="InternalFlowActivityListener"/> that
    /// captures <see cref="System.Diagnostics.Activity"/> spans for internal flow diagram popups.
    /// <para>
    /// This uses a raw <see cref="System.Diagnostics.ActivityListener"/> (BCL)
    /// rather than the OTel SDK, so it works with or without an existing
    /// OpenTelemetry configuration and never interferes with the SUT's
    /// sampling, exporters, or telemetry assertions.
    /// </para>
    /// <para>
    /// <b>Note:</b> The <see cref="Tracking.TestTrackingMessageHandler"/> auto-starts a listener
    /// for well-known sources automatically. This method is only needed if you want to register
    /// additional custom <see cref="System.Diagnostics.ActivitySource"/>s via DI, or if you
    /// need the listener started before any handler is constructed.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection (typically from <c>ConfigureTestServices</c>).</param>
    /// <param name="additionalActivitySources">
    /// Optional custom activity source names to capture in addition to the
    /// well-known auto-instrumentation sources.
    /// </param>
    public static IServiceCollection AddActivityListenerForInternalFlowTracking(
        this IServiceCollection services,
        params string[] additionalActivitySources)
    {
        // Eagerly create so the listener starts capturing immediately — 
        // it must be active before the host processes any requests.
        var listener = new InternalFlowActivityListener(additionalActivitySources);

        // Register as a factory returning the already-created instance.
        // Factory-registered singletons ARE disposed by the container.
        services.AddSingleton(_ => listener);
        return services;
    }
}
