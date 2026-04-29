using MassTransit;

namespace TestTrackingDiagrams.Extensions.MassTransit;

/// <summary>
/// Classifies MassTransit HTTP requests into specific operations based on URL patterns and HTTP methods.
/// </summary>
public static class MassTransitOperationClassifier
{
    public static MassTransitOperationInfo ClassifySend<T>(SendContext<T> context) where T : class
    {
        return new(
            MassTransitOperation.Send,
            typeof(T).Name,
            context.DestinationAddress,
            context.SourceAddress,
            context.MessageId,
            context.ConversationId);
    }

    public static MassTransitOperationInfo ClassifyPublish<T>(PublishContext<T> context) where T : class
    {
        return new(
            MassTransitOperation.Publish,
            typeof(T).Name,
            context.DestinationAddress,
            context.SourceAddress,
            context.MessageId,
            context.ConversationId);
    }

    public static MassTransitOperationInfo ClassifyConsume<T>(ConsumeContext<T> context) where T : class
    {
        return new(
            MassTransitOperation.Consume,
            typeof(T).Name,
            context.ReceiveContext?.InputAddress,
            context.SourceAddress,
            context.MessageId,
            context.ConversationId);
    }

    public static string GetDiagramLabel(MassTransitOperationInfo op, MassTransitTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            MassTransitTrackingVerbosity.Raw =>
                $"{op.Operation} {op.MessageType} → {op.DestinationAddress}",
            MassTransitTrackingVerbosity.Detailed => op.Operation switch
            {
                MassTransitOperation.Send => $"Send {op.MessageType}",
                MassTransitOperation.Publish => $"Publish {op.MessageType}",
                MassTransitOperation.Consume => $"Consume {op.MessageType}",
                MassTransitOperation.SendFault => $"Send Fault {op.MessageType}",
                MassTransitOperation.PublishFault => $"Publish Fault {op.MessageType}",
                MassTransitOperation.ConsumeFault => $"Consume Fault {op.MessageType}",
                _ => op.Operation.ToString()
            },
            MassTransitTrackingVerbosity.Summarised => op.Operation switch
            {
                MassTransitOperation.Send or MassTransitOperation.Publish => "→ " + op.MessageType,
                MassTransitOperation.Consume => "← " + op.MessageType,
                _ => op.Operation.ToString()
            },
            _ => op.Operation.ToString()
        };
    }

    public static Uri BuildUri(MassTransitOperationInfo op, MassTransitTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            MassTransitTrackingVerbosity.Raw when op.DestinationAddress is not null =>
                op.DestinationAddress,
            MassTransitTrackingVerbosity.Detailed when op.DestinationAddress is not null =>
                new Uri($"masstransit:///{ExtractQueueName(op.DestinationAddress)}"),
            MassTransitTrackingVerbosity.Summarised when op.MessageType is not null =>
                new Uri($"masstransit:///{op.MessageType}"),
            _ => new Uri("masstransit:///unknown")
        };
    }

    private static string ExtractQueueName(Uri uri)
    {
        // MassTransit URIs like "rabbitmq://localhost/orders-queue" or "sb://namespace/queue"
        var lastSegment = uri.Segments.LastOrDefault()?.TrimEnd('/');
        return string.IsNullOrEmpty(lastSegment) ? uri.Host : lastSegment;
    }
}