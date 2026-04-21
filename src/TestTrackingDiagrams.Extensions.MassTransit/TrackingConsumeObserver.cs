using MassTransit;

namespace TestTrackingDiagrams.Extensions.MassTransit;

public class TrackingConsumeObserver(MassTransitTracker tracker) : IConsumeObserver
{
    public Task PreConsume<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

    public Task PostConsume<T>(ConsumeContext<T> context) where T : class
    {
        var op = MassTransitOperationClassifier.ClassifyConsume(context);
        tracker.LogConsume(op, context.Message);
        return Task.CompletedTask;
    }

    public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
    {
        var op = new MassTransitOperationInfo(
            MassTransitOperation.ConsumeFault, typeof(T).Name,
            context.ReceiveContext?.InputAddress, context.SourceAddress,
            context.MessageId, context.ConversationId);
        tracker.LogConsumeFault(op, exception);
        return Task.CompletedTask;
    }
}
