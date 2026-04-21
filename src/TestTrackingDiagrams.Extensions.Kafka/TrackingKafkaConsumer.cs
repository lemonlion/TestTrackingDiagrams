using Confluent.Kafka;

namespace TestTrackingDiagrams.Extensions.Kafka;

public class TrackingKafkaConsumer<TKey, TValue> : IConsumer<TKey, TValue>
{
    private readonly IConsumer<TKey, TValue> _inner;
    private readonly KafkaTracker _tracker;
    private readonly KafkaTrackingOptions _options;

    public TrackingKafkaConsumer(IConsumer<TKey, TValue> inner, KafkaTracker tracker, KafkaTrackingOptions options)
    {
        _inner = inner;
        _tracker = tracker;
        _options = options;
    }

    public ConsumeResult<TKey, TValue> Consume(int millisecondsTimeout)
    {
        var result = _inner.Consume(millisecondsTimeout);
        LogConsumeResult(result);
        return result;
    }

    public ConsumeResult<TKey, TValue> Consume(TimeSpan timeout)
    {
        var result = _inner.Consume(timeout);
        LogConsumeResult(result);
        return result;
    }

    public ConsumeResult<TKey, TValue> Consume(CancellationToken cancellationToken = default)
    {
        var result = _inner.Consume(cancellationToken);
        LogConsumeResult(result);
        return result;
    }

    public void Subscribe(string topic)
    {
        _inner.Subscribe(topic);

        if (_options.TrackSubscribe)
        {
            var op = new KafkaOperationInfo(KafkaOperation.Subscribe, topic);
            _tracker.LogSubscribe(op);
        }
    }

    public void Subscribe(IEnumerable<string> topics)
    {
        _inner.Subscribe(topics);

        if (_options.TrackSubscribe)
        {
            foreach (var topic in topics)
            {
                var op = new KafkaOperationInfo(KafkaOperation.Subscribe, topic);
                _tracker.LogSubscribe(op);
            }
        }
    }

    private void LogConsumeResult(ConsumeResult<TKey, TValue>? result)
    {
        if (result is null || result.IsPartitionEOF) return;

        var op = new KafkaOperationInfo(KafkaOperation.Consume, result.Topic,
            result.Partition.Value, result.Offset.Value);

        string? content = null;
        if (_options.LogMessageValue && result.Message is not null && result.Message.Value is not null)
            content = result.Message.Value.ToString();

        _tracker.LogConsume(op, content);
    }

    // ─── IConsumer<TKey, TValue> delegation ───────────────────

    public Handle Handle => _inner.Handle;
    public string Name => _inner.Name;
    public string MemberId => _inner.MemberId;
    public List<TopicPartition> Assignment => _inner.Assignment;
    public List<string> Subscription => _inner.Subscription;
    public IConsumerGroupMetadata ConsumerGroupMetadata => _inner.ConsumerGroupMetadata;
    public int AddBrokers(string brokers) => _inner.AddBrokers(brokers);
    public void SetSaslCredentials(string username, string password) => _inner.SetSaslCredentials(username, password);
    public void Unsubscribe() => _inner.Unsubscribe();
    public void Assign(TopicPartition partition) => _inner.Assign(partition);
    public void Assign(TopicPartitionOffset partition) => _inner.Assign(partition);
    public void Assign(IEnumerable<TopicPartition> partitions) => _inner.Assign(partitions);
    public void Assign(IEnumerable<TopicPartitionOffset> partitions) => _inner.Assign(partitions);
    public void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions) => _inner.IncrementalAssign(partitions);
    public void IncrementalAssign(IEnumerable<TopicPartition> partitions) => _inner.IncrementalAssign(partitions);
    public void IncrementalUnassign(IEnumerable<TopicPartition> partitions) => _inner.IncrementalUnassign(partitions);
    public void Unassign() => _inner.Unassign();
    public void StoreOffset(ConsumeResult<TKey, TValue> result) => _inner.StoreOffset(result);
    public void StoreOffset(TopicPartitionOffset offset) => _inner.StoreOffset(offset);
    public List<TopicPartitionOffset> Commit() => _inner.Commit();
    public void Commit(IEnumerable<TopicPartitionOffset> offsets) => _inner.Commit(offsets);
    public void Commit(ConsumeResult<TKey, TValue> result) => _inner.Commit(result);
    public void Seek(TopicPartitionOffset tpo) => _inner.Seek(tpo);
    public void Pause(IEnumerable<TopicPartition> partitions) => _inner.Pause(partitions);
    public void Resume(IEnumerable<TopicPartition> partitions) => _inner.Resume(partitions);
    public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout) => _inner.Committed(partitions, timeout);
    public List<TopicPartitionOffset> Committed(TimeSpan timeout) => _inner.Committed(timeout);
    public Offset Position(TopicPartition partition) => _inner.Position(partition);
    public List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout) => _inner.OffsetsForTimes(timestampsToSearch, timeout);
    public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition) => _inner.GetWatermarkOffsets(topicPartition);
    public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout) => _inner.QueryWatermarkOffsets(topicPartition, timeout);
    public void Close() => _inner.Close();
    public void Dispose() => _inner.Dispose();
}
