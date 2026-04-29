using Confluent.Kafka;
using TestTrackingDiagrams.Extensions.Kafka;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Kafka;

public class TrackingKafkaConsumerFactoryTests
{
    private readonly string _testId = Guid.NewGuid().ToString();

    private RequestResponseLog[] GetLogsFromThisTest()
    {
        return RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == _testId)
            .ToArray();
    }

    private KafkaTrackingOptions MakeOptions() => new()
    {
        ServiceName = "Kafka",
        CallerName = "TestCaller",
        CurrentTestInfoFetcher = () => ("Factory Test", _testId),
    };

    // ─── TrackingKafkaConsumerFactory ───────────────────────

    [Fact]
    public void Create_Returns_TrackingKafkaConsumer()
    {
        var options = MakeOptions();
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumerFactory<string, string>();
        var factory = new TrackingKafkaConsumerFactory<string, string>(inner, tracker, options);

        var consumer = factory.Create(config => { });

        Assert.IsType<TrackingKafkaConsumer<string, string>>(consumer);
    }

    [Fact]
    public void Create_Delegates_To_Inner_Factory()
    {
        var options = MakeOptions();
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumerFactory<string, string>();
        var factory = new TrackingKafkaConsumerFactory<string, string>(inner, tracker, options);

        factory.Create(config => { });

        Assert.True(inner.CreateCalled);
    }

    [Fact]
    public void Create_Passes_Configure_Action_To_Inner()
    {
        var options = MakeOptions();
        var tracker = new KafkaTracker(options);
        var inner = new FakeConsumerFactory<string, string>();
        var factory = new TrackingKafkaConsumerFactory<string, string>(inner, tracker, options);

        ConsumerConfig? capturedConfig = null;
        factory.Create(config =>
        {
            config.GroupId = "test-group";
            capturedConfig = config;
        });

        Assert.NotNull(capturedConfig);
        Assert.Equal("test-group", capturedConfig!.GroupId);
    }

    [Fact]
    public void Created_Consumer_Tracks_Consume_Operations()
    {
        var options = MakeOptions();
        var tracker = new KafkaTracker(options);
        var fakeInner = new FakeConsumer<string, string>();
        fakeInner.NextConsumeResult = new ConsumeResult<string, string>
        {
            Topic = "orders-topic",
            Partition = new Partition(0),
            Offset = new Offset(42),
            Message = new Message<string, string> { Key = "k1", Value = "v1" }
        };
        var inner = new FakeConsumerFactory<string, string> { ConsumerToReturn = fakeInner };
        var factory = new TrackingKafkaConsumerFactory<string, string>(inner, tracker, options);

        var consumer = factory.Create(config => { });
        consumer.Consume(TimeSpan.FromSeconds(1));

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    // ─── KafkaConsumerFactory (default) ─────────────────────

    [Fact]
    public void Default_Factory_Implements_Interface()
    {
        var factory = new KafkaConsumerFactory<string, string>();
        Assert.IsAssignableFrom<IKafkaConsumerFactory<string, string>>(factory);
    }

    #region Test Doubles

    private class FakeConsumerFactory<TKey, TValue> : IKafkaConsumerFactory<TKey, TValue>
    {
        public bool CreateCalled { get; private set; }
        public FakeConsumer<TKey, TValue>? ConsumerToReturn { get; set; }

        public IConsumer<TKey, TValue> Create(Action<ConsumerConfig> configure)
        {
            CreateCalled = true;
            var config = new ConsumerConfig();
            configure(config);
            return ConsumerToReturn ?? new FakeConsumer<TKey, TValue>();
        }
    }

    private class FakeConsumer<TKey, TValue> : IConsumer<TKey, TValue>
    {
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
        public void Unsubscribe() { }
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
        public List<TopicPartitionOffset> Commit() => [];
        public void Commit(IEnumerable<TopicPartitionOffset> offsets) { }
        public void Commit(ConsumeResult<TKey, TValue> result) { }
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
