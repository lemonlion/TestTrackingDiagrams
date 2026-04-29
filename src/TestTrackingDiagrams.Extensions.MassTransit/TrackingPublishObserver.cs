using MassTransit;

namespace TestTrackingDiagrams.Extensions.MassTransit;

/// <summary>
/// MassTransit observer that logs operations to the tracking system.
/// </summary>
public class TrackingPublishObserver(MassTransitTracker tracker) : IPublishObserver
{
    public Task PrePublish<T>(PublishContext<T> context) where T : class => Task.CompletedTask;

    public Task PostPublish<T>(PublishContext<T> context) where T : class
    {
        var op = MassTransitOperationClassifier.ClassifyPublish(context);
        tracker.LogPublish(op, context.Message);
        return Task.CompletedTask;
    }

    public Task PublishFault<T>(PublishContext<T> context, Exception exception) where T : class
    {
        var op = new MassTransitOperationInfo(
            MassTransitOperation.PublishFault, typeof(T).Name,
            context.DestinationAddress, context.SourceAddress,
            context.MessageId, context.ConversationId);
        tracker.LogPublishFault(op, exception);
        return Task.CompletedTask;
    }
}
