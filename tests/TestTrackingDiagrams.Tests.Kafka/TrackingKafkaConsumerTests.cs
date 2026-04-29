using Confluent.Kafka;
using TestTrackingDiagrams.Extensions.Kafka;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Kafka;

public class TrackingKafkaConsumerTests
{
    private readonly string _testId = Guid.NewGuid().ToString();

    private RequestResponseLog[] GetLogsFromThisTest()
    {
        return RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == _testId)
            .ToArray();
    }

    private KafkaTrackingOptions MakeOptions(
        KafkaTrackingVerbosity verbosity = KafkaTrackingVerbosity.Detailed) => new()
    {
        ServiceName = "Kafka",
        CallerName = "TestCaller",
        Verbosity = verbosity,
        CurrentTestInfoFetcher = () => ("My Kafka Test", _testId),
    };

    // ─── Commit ─────────────────────────────────────────────

    [Fact]
    public void Commit_NoArgs_Tracks_when_TrackCommit_true()
    {
        var options = MakeOptions();
        options.TrackCommit = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumer<string, string>();
        var consumer = new TrackingKafkaConsumer<string, string>(inner, tracker, options);

        consumer.Commit();

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void Commit_Offsets_Tracks_when_TrackCommit_true()
    {
        var options = MakeOptions();
        options.TrackCommit = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumer<string, string>();
        var consumer = new TrackingKafkaConsumer<string, string>(inner, tracker, options);

        consumer.Commit(new List<TopicPartitionOffset>
        {
            new("orders-topic", 0, 42)
        });

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void Commit_ConsumeResult_Tracks_when_TrackCommit_true()
    {
        var options = MakeOptions();
        options.TrackCommit = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumer<string, string>();
        var consumer = new TrackingKafkaConsumer<string, string>(inner, tracker, options);

        consumer.Commit(new ConsumeResult<string, string>
        {
            Topic = "orders-topic",
            Partition = new Partition(0),
            Offset = new Offset(42),
        });

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void Commit_DoesNotTrack_when_TrackCommit_false()
    {
        var options = MakeOptions();
        options.TrackCommit = false;
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumer<string, string>();
        var consumer = new TrackingKafkaConsumer<string, string>(inner, tracker, options);

        consumer.Commit();

        Assert.Empty(GetLogsFromThisTest());
    }

    [Fact]
    public void Commit_Still_delegates_to_inner()
    {
        var options = MakeOptions();
        options.TrackCommit = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumer<string, string>();
        var consumer = new TrackingKafkaConsumer<string, string>(inner, tracker, options);

        consumer.Commit();

        Assert.True(inner.CommitCalled);
    }

    // ─── Unsubscribe ────────────────────────────────────────

    [Fact]
    public void Unsubscribe_Tracks_when_TrackUnsubscribe_true()
    {
        var options = MakeOptions();
        options.TrackUnsubscribe = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumer<string, string>();
        var consumer = new TrackingKafkaConsumer<string, string>(inner, tracker, options);

        consumer.Unsubscribe();

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void Unsubscribe_DoesNotTrack_when_TrackUnsubscribe_false()
    {
        var options = MakeOptions();
        options.TrackUnsubscribe = false;
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumer<string, string>();
        var consumer = new TrackingKafkaConsumer<string, string>(inner, tracker, options);

        consumer.Unsubscribe();

        Assert.Empty(GetLogsFromThisTest());
    }

    [Fact]
    public void Unsubscribe_Still_delegates_to_inner()
    {
        var options = MakeOptions();
        options.TrackUnsubscribe = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumer<string, string>();
        var consumer = new TrackingKafkaConsumer<string, string>(inner, tracker, options);

        consumer.Unsubscribe();

        Assert.True(inner.UnsubscribeCalled);
    }

    // ─── Consume content: Key logging ───────────────────────

    [Fact]
    public void Consume_Includes_Key_in_content()
    {
        var options = MakeOptions();
        options.LogMessageKey = true;
        options.LogMessageValue = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumer<string, string>();
        inner.NextConsumeResult = new ConsumeResult<string, string>
        {
            Topic = "orders-topic",
            Partition = new Partition(0),
            Offset = new Offset(42),
            Message = new Message<string, string> { Key = "order-123", Value = "{\"amount\":99}" }
        };
        var consumer = new TrackingKafkaConsumer<string, string>(inner, tracker, options);

        consumer.Consume(TimeSpan.FromSeconds(1));

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("order-123", log.Content);
        Assert.Contains("{\"amount\":99}", log.Content);
    }

    [Fact]
    public void Consume_Omits_Key_when_LogMessageKey_false()
    {
        var options = MakeOptions();
        options.LogMessageKey = false;
        options.LogMessageValue = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumer<string, string>();
        inner.NextConsumeResult = new ConsumeResult<string, string>
        {
            Topic = "orders-topic",
            Partition = new Partition(0),
            Offset = new Offset(42),
            Message = new Message<string, string> { Key = "order-123", Value = "{\"amount\":99}" }
        };
        var consumer = new TrackingKafkaConsumer<string, string>(inner, tracker, options);

        consumer.Consume(TimeSpan.FromSeconds(1));

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.DoesNotContain("order-123", log.Content);
        Assert.Contains("{\"amount\":99}", log.Content);
    }

    [Fact]
    public void Consume_Omits_content_in_Summarised_mode()
    {
        var options = MakeOptions(KafkaTrackingVerbosity.Summarised);
        options.LogMessageKey = true;
        options.LogMessageValue = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumer<string, string>();
        inner.NextConsumeResult = new ConsumeResult<string, string>
        {
            Topic = "orders-topic",
            Partition = new Partition(0),
            Offset = new Offset(42),
            Message = new Message<string, string> { Key = "order-123", Value = "{\"amount\":99}" }
        };
        var consumer = new TrackingKafkaConsumer<string, string>(inner, tracker, options);

        consumer.Consume(TimeSpan.FromSeconds(1));

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Null(log.Content);
    }

    #region Test Double

    private class FakeConsumer<TKey, TValue> : IConsumer<TKey, TValue>
    {
        public bool CommitCalled { get; private set; }
        public bool UnsubscribeCalled { get; private set; }
        public ConsumeResult<TKey, TValue>? NextConsumeResult { get; set; }

        public Handle Handle => throw new NotImplementedException();
        public string Name => "fake";
        public string MemberId => "fake";
        public List<TopicPartition> Assignment => [];
        public List<string> Subscription => [];
        public IConsumerGroupMetadata ConsumerGroupMetadata => throw new NotImplementedException();
        public void SetSaslCredentials(string username, string password) { }
        public void Dispose() { }
        public int AddBrokers(string brokers) => 0;
        public ConsumeResult<TKey, TValue> Consume(int millisecondsTimeout) => NextConsumeResult!;
        public ConsumeResult<TKey, TValue> Consume(CancellationToken cancellationToken = default) => NextConsumeResult!;
        public ConsumeResult<TKey, TValue> Consume(TimeSpan timeout) => NextConsumeResult!;
        public void Subscribe(IEnumerable<string> topics) { }
        public void Subscribe(string topic) { }
        public void Unsubscribe() { UnsubscribeCalled = true; }
        public void Assign(TopicPartition partition) { }
        public void Assign(TopicPartitionOffset partition) { }
        public void Assign(IEnumerable<TopicPartition> partitions) { }
        public void Assign(IEnumerable<TopicPartitionOffset> partitions) { }
        public void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions) { }
        public void IncrementalAssign(IEnumerable<TopicPartition> partitions) { }
        public void IncrementalUnassign(IEnumerable<TopicPartition> partitions) { }
        public void Unassign() { }
        public void StoreOffset(ConsumeResult<TKey, TValue> result) { }
        public void StoreOffset(TopicPartitionOffset offset) { }
        public List<TopicPartitionOffset> Commit() { CommitCalled = true; return []; }
        public void Commit(IEnumerable<TopicPartitionOffset> offsets) { CommitCalled = true; }
        public void Commit(ConsumeResult<TKey, TValue> result) { CommitCalled = true; }
        public void Seek(TopicPartitionOffset tpo) { }
        public void Pause(IEnumerable<TopicPartition> partitions) { }
        public void Resume(IEnumerable<TopicPartition> partitions) { }
        public List<TopicPartitionOffset> Committed(TimeSpan timeout) => [];
        public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout) => [];
        public Offset Position(TopicPartition partition) => Offset.Unset;
        public List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout) => [];
        public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition) => new(Offset.Unset, Offset.Unset);
        public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout) => new(Offset.Unset, Offset.Unset);
        public void Close() { }
    }

    #endregion
}
