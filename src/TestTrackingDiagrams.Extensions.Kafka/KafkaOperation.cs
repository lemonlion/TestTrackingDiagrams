namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Classified Kafka operation types.
/// </summary>
public enum KafkaOperation
{
    Produce,
    ProduceAsync,
    Consume,
    Subscribe,
    Unsubscribe,
    Commit,
    Flush,
    InitTransactions,
    BeginTransaction,
    CommitTransaction,
    AbortTransaction,
    SendOffsetsToTransaction,
    Other
}
