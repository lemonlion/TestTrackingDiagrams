using Example.Api.Events;
using TestTrackingDiagrams.Tracking;

namespace Example.Api.Tests.Component.Shared.Fakes;

public class FakeEventPublisher(MessageTracker tracker) : IEventPublisher
{
    public Task PublishAsync(CakeCreatedEvent @event)
    {
        var correlationId = tracker.TrackMessageRequest(
            protocol: "Event",
            destinationName: "Event broker",
            destinationUri: new Uri("event://cake-created"),
            payload: @event);

        tracker.TrackMessageResponse(
            protocol: "Event",
            destinationName: "Event broker",
            destinationUri: new Uri("event://cake-created"),
            requestResponseId: correlationId);

        return Task.CompletedTask;
    }
}
