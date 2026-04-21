namespace TestTrackingDiagrams.Extensions.Kafka;

public record KafkaOperationInfo(
    KafkaOperation Operation,
    string? Topic = null,
    int? Partition = null,
    long? Offset = null);
