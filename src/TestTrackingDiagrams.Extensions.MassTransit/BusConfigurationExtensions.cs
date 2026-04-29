using MassTransit;

namespace TestTrackingDiagrams.Extensions.MassTransit;

/// <summary>
/// Provides extension methods for configuring MassTransit bus instances to enable test tracking.
/// </summary>
public static class BusConfigurationExtensions
{
    public static MassTransitTracker WithTestTracking(
        this IBusFactoryConfigurator configurator,
        MassTransitTrackingOptions options)
    {
        var tracker = new MassTransitTracker(options, options.HttpContextAccessor);

        if (options.TrackSend)
            configurator.ConnectSendObserver(new TrackingSendObserver(tracker));
        if (options.TrackPublish)
            configurator.ConnectPublishObserver(new TrackingPublishObserver(tracker));
        if (options.TrackConsume)
            configurator.ConnectConsumeObserver(new TrackingConsumeObserver(tracker));

        return tracker;
    }
}