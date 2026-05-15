namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Standardized correlation key format helpers. Ensures consistent key construction
/// between auto-population (at write time) and resolution (at processing time).
/// </summary>
public static class CorrelationKeys
{
    /// <summary>
    /// Creates a correlation key for a Cosmos DB document.
    /// </summary>
    /// <param name="serviceName">The service/database name used in tracking options.</param>
    /// <param name="documentId">The document ID.</param>
    public static string Cosmos(string serviceName, string documentId)
        => $"cosmos:{serviceName}:{documentId}";

    /// <summary>
    /// Creates a correlation key for a Cosmos DB document with partition key.
    /// </summary>
    /// <param name="serviceName">The service/database name used in tracking options.</param>
    /// <param name="partitionKey">The partition key value.</param>
    /// <param name="documentId">The document ID.</param>
    public static string Cosmos(string serviceName, string partitionKey, string documentId)
        => $"cosmos:{serviceName}:{partitionKey}:{documentId}";

    /// <summary>
    /// Creates a correlation key for a MongoDB document.
    /// </summary>
    /// <param name="serviceName">The service/database name used in tracking options.</param>
    /// <param name="documentId">The document ID (typically _id as string).</param>
    public static string Mongo(string serviceName, string documentId)
        => $"mongo:{serviceName}:{documentId}";

    /// <summary>
    /// Creates a correlation key for a Kafka message.
    /// </summary>
    /// <param name="serviceName">The topic/service name used in tracking options.</param>
    /// <param name="messageKey">The message key.</param>
    public static string Kafka(string serviceName, string messageKey)
        => $"kafka:{serviceName}:{messageKey}";

    /// <summary>
    /// Creates a correlation key for a Service Bus message.
    /// </summary>
    /// <param name="serviceName">The queue/topic name used in tracking options.</param>
    /// <param name="messageId">The message ID or session ID.</param>
    public static string ServiceBus(string serviceName, string messageId)
        => $"servicebus:{serviceName}:{messageId}";

    /// <summary>
    /// Creates a correlation key for an Event Hubs event.
    /// </summary>
    /// <param name="serviceName">The event hub name used in tracking options.</param>
    /// <param name="eventId">The event identifier (e.g. sequence number or partition key).</param>
    public static string EventHubs(string serviceName, string eventId)
        => $"eventhubs:{serviceName}:{eventId}";

    /// <summary>
    /// Creates a correlation key for a Google Pub/Sub message.
    /// </summary>
    /// <param name="serviceName">The topic/subscription name used in tracking options.</param>
    /// <param name="messageId">The message ID.</param>
    public static string PubSub(string serviceName, string messageId)
        => $"pubsub:{serviceName}:{messageId}";

    /// <summary>
    /// Creates a correlation key for an AWS SQS message.
    /// </summary>
    /// <param name="serviceName">The queue name used in tracking options.</param>
    /// <param name="messageId">The SQS message ID.</param>
    public static string Sqs(string serviceName, string messageId)
        => $"sqs:{serviceName}:{messageId}";

    /// <summary>
    /// Creates a correlation key for an AWS SNS message.
    /// </summary>
    /// <param name="serviceName">The topic name used in tracking options.</param>
    /// <param name="messageId">The SNS message ID.</param>
    public static string Sns(string serviceName, string messageId)
        => $"sns:{serviceName}:{messageId}";

    /// <summary>
    /// Creates a correlation key for a Storage Queue message.
    /// </summary>
    /// <param name="serviceName">The queue name used in tracking options.</param>
    /// <param name="messageId">The message ID.</param>
    public static string StorageQueue(string serviceName, string messageId)
        => $"storagequeue:{serviceName}:{messageId}";

    /// <summary>
    /// Creates a custom correlation key with a user-defined prefix.
    /// Use this for custom background processors not covered by built-in helpers.
    /// </summary>
    /// <param name="prefix">The system prefix (e.g. "hangfire", "channel").</param>
    /// <param name="serviceName">The service/processor name.</param>
    /// <param name="itemId">The work-item identifier.</param>
    public static string Custom(string prefix, string serviceName, string itemId)
        => $"{prefix}:{serviceName}:{itemId}";
}
