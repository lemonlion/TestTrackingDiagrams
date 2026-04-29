using Confluent.Kafka;

namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Tracking wrapper for Kafka operations. Intercepts calls and logs them for test diagrams.
/// </summary>
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

        _tracker.LogConsume(op, BuildContent(result.Message));
    }

    private string? BuildContent(Message<TKey, TValue>? message)
    {
        if (message is null) return null;
        if (_options.Verbosity == KafkaTrackingVerbosity.Summarised) return null;

        var parts = new List<string>();
        if (_options.LogMessageKey && message.Key is not null)
            parts.Add($"Key: {message.Key}");
        if (_options.LogMessageValue && message.Value is not null)
            parts.Add($"Value: {message.Value}");
        return parts.Count > 0 ? string.Join(", ", parts) : null;
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
    public void Unsubscribe()
    {
        _inner.Unsubscribe();

        if (_options.TrackUnsubscribe)
        {
            var op = new KafkaOperationInfo(KafkaOperation.Unsubscribe);
            _tracker.LogUnsubscribe(op);
        }
    }
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
    public List<TopicPartitionOffset> Commit()
    {
        var result = _inner.Commit();
        LogCommit();
        return result;
    }

    public void Commit(IEnumerable<TopicPartitionOffset> offsets)
    {
        _inner.Commit(offsets);
        LogCommit();
    }

    public void Commit(ConsumeResult<TKey, TValue> result)
    {
        _inner.Commit(result);
        LogCommit();
    }

    private void LogCommit()
    {
        if (_options.TrackCommit)
        {
            var op = new KafkaOperationInfo(KafkaOperation.Commit);
            _tracker.LogCommit(op);
        }
    }
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
