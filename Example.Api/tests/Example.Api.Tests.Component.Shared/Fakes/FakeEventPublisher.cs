using Example.Api.Events;
using TestTrackingDiagrams.Tracking;

namespace Example.Api.Tests.Component.Shared.Fakes;

public class FakeEventPublisher(MessageTracker tracker) : IEventPublisher
{
    public Task PublishAsync(CakeCreatedEvent @event)
    {
        var correlationId = tracker.TrackMessageRequest(
            protocol: "Send (Event Protocol)",
            destinationName: "Event broker",
            destinationUri: new Uri("event://event-broker/cake_events"),
            payload: @event);

        tracker.TrackMessageResponse(
            protocol: "Send (Event Protocol)",
            destinationName: "Event broker",
            destinationUri: new Uri("event://event-broker/cake_events"),
            requestResponseId: correlationId);

        return Task.CompletedTask;
    }
}
