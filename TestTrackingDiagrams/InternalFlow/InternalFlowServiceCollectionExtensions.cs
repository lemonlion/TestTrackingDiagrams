using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.InternalFlow;

public static class InternalFlowServiceCollectionExtensions
{
    /// <summary>
    /// Registers a non-invasive <see cref="InternalFlowActivityListener"/> that
    /// captures OpenTelemetry spans for internal flow diagram popups.
    /// <para>
    /// This uses a raw <see cref="System.Diagnostics.ActivityListener"/> (BCL)
    /// rather than the OTel SDK, so it works with or without an existing
    /// OpenTelemetry configuration and never interferes with the SUT's
    /// sampling, exporters, or telemetry assertions.
    /// </para>
    /// <para>
    /// All well-known auto-instrumentation sources (ASP.NET Core, HttpClient,
    /// EF Core, Redis, Cosmos, etc.) are subscribed automatically.
    /// Pass additional source names to capture custom <see cref="System.Diagnostics.ActivitySource"/>s.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection (typically from <c>ConfigureTestServices</c>).</param>
    /// <param name="additionalActivitySources">
    /// Optional custom activity source names to capture in addition to the
    /// well-known auto-instrumentation sources.
    /// </param>
    public static IServiceCollection AddOpenTelemetryForInternalFlowTracking(
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
