using MassTransit;

namespace TestTrackingDiagrams.Extensions.MassTransit;

public class TrackingSendObserver(MassTransitTracker tracker) : ISendObserver
{
    public Task PreSend<T>(SendContext<T> context) where T : class => Task.CompletedTask;

    public Task PostSend<T>(SendContext<T> context) where T : class
    {
        var op = MassTransitOperationClassifier.ClassifySend(context);
        tracker.LogSend(op, context.Message);
        return Task.CompletedTask;
    }

    public Task SendFault<T>(SendContext<T> context, Exception exception) where T : class
    {
        var op = new MassTransitOperationInfo(
            MassTransitOperation.SendFault, typeof(T).Name,
            context.DestinationAddress, context.SourceAddress,
            context.MessageId, context.ConversationId);
        tracker.LogSendFault(op, exception);
        return Task.CompletedTask;
    }
}
