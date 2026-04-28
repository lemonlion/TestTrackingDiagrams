namespace TestTrackingDiagrams.Extensions.Kafka;

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
