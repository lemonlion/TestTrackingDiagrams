namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// The result of classifying a Kafka operation, containing the operation type and metadata.
/// </summary>
public record KafkaOperationInfo(
    KafkaOperation Operation,
    string? Topic = null,
    int? Partition = null,
    long? Offset = null);
