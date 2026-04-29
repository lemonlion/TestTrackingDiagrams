using Confluent.Kafka;
using TestTrackingDiagrams.Extensions.Kafka;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Kafka;

public class TrackingKafkaProducerTests
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
        Verbosity = KafkaTrackingVerbosity.Detailed,
        CurrentTestInfoFetcher = () => ("My Kafka Test", _testId),
    };

    // ─── Flush ──────────────────────────────────────────────

    [Fact]
    public void Flush_TimeSpan_Tracks_when_TrackFlush_true()
    {
        var options = MakeOptions();
        options.TrackFlush = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.Flush(TimeSpan.FromSeconds(1));

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void Flush_CancellationToken_Tracks_when_TrackFlush_true()
    {
        var options = MakeOptions();
        options.TrackFlush = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.Flush(CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void Flush_DoesNotTrack_when_TrackFlush_false()
    {
        var options = MakeOptions();
        options.TrackFlush = false;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.Flush(TimeSpan.FromSeconds(1));

        Assert.Empty(GetLogsFromThisTest());
    }

    [Fact]
    public void Flush_Still_delegates_to_inner()
    {
        var options = MakeOptions();
        options.TrackFlush = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.Flush(TimeSpan.FromSeconds(1));

        Assert.True(inner.FlushCalled);
    }

    // ─── Transactions ───────────────────────────────────────

    [Fact]
    public void InitTransactions_Tracks_when_TrackTransactions_true()
    {
        var options = MakeOptions();
        options.TrackTransactions = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.InitTransactions(TimeSpan.FromSeconds(5));

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.True(inner.InitTransactionsCalled);
    }

    [Fact]
    public void BeginTransaction_Tracks_when_TrackTransactions_true()
    {
        var options = MakeOptions();
        options.TrackTransactions = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.BeginTransaction();

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.True(inner.BeginTransactionCalled);
    }

    [Fact]
    public void CommitTransaction_Tracks_when_TrackTransactions_true()
    {
        var options = MakeOptions();
        options.TrackTransactions = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.CommitTransaction();

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.True(inner.CommitTransactionCalled);
    }

    [Fact]
    public void CommitTransaction_WithTimeout_Tracks_when_TrackTransactions_true()
    {
        var options = MakeOptions();
        options.TrackTransactions = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.CommitTransaction(TimeSpan.FromSeconds(5));

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void AbortTransaction_Tracks_when_TrackTransactions_true()
    {
        var options = MakeOptions();
        options.TrackTransactions = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.AbortTransaction();

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.True(inner.AbortTransactionCalled);
    }

    [Fact]
    public void AbortTransaction_WithTimeout_Tracks_when_TrackTransactions_true()
    {
        var options = MakeOptions();
        options.TrackTransactions = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.AbortTransaction(TimeSpan.FromSeconds(5));

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void SendOffsetsToTransaction_Tracks_when_TrackTransactions_true()
    {
        var options = MakeOptions();
        options.TrackTransactions = true;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.SendOffsetsToTransaction(
            [new TopicPartitionOffset("orders-topic", 0, 42)],
            null!, TimeSpan.FromSeconds(5));

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.True(inner.SendOffsetsCalled);
    }

    [Fact]
    public void Transactions_DoNotTrack_when_TrackTransactions_false()
    {
        var options = MakeOptions();
        options.TrackTransactions = false;
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducer<string, string>();
        var producer = new TrackingKafkaProducer<string, string>(inner, tracker, options);

        producer.InitTransactions(TimeSpan.FromSeconds(5));
        producer.BeginTransaction();
        producer.CommitTransaction();
        producer.AbortTransaction();

        Assert.Empty(GetLogsFromThisTest());
    }

    #region Test Double

    private class FakeProducer<TKey, TValue> : IProducer<TKey, TValue>
    {
        public bool FlushCalled { get; private set; }
        public bool InitTransactionsCalled { get; private set; }
        public bool BeginTransactionCalled { get; private set; }
        public bool CommitTransactionCalled { get; private set; }
        public bool AbortTransactionCalled { get; private set; }
        public bool SendOffsetsCalled { get; private set; }

        public Handle Handle => throw new NotImplementedException();
        public string Name => "fake";
        public void SetSaslCredentials(string username, string password) { }
        public void Dispose() { }
        public int AddBrokers(string brokers) => 0;
        public void InitTransactions(TimeSpan timeout) { InitTransactionsCalled = true; }
        public void BeginTransaction() { BeginTransactionCalled = true; }
        public void CommitTransaction(TimeSpan timeout) { CommitTransactionCalled = true; }
        public void CommitTransaction() { CommitTransactionCalled = true; }
        public void AbortTransaction(TimeSpan timeout) { AbortTransactionCalled = true; }
        public void AbortTransaction() { AbortTransactionCalled = true; }
        public void SendOffsetsToTransaction(IEnumerable<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata, TimeSpan timeout) { SendOffsetsCalled = true; }
        public int Flush(TimeSpan timeout) { FlushCalled = true; return 0; }
        public void Flush(CancellationToken cancellationToken = default) { FlushCalled = true; }
        public int Poll(TimeSpan timeout) => 0;
        public void Produce(string topic, Message<TKey, TValue> message, Action<DeliveryReport<TKey, TValue>>? deliveryHandler = null) { }
        public void Produce(TopicPartition topicPartition, Message<TKey, TValue> message, Action<DeliveryReport<TKey, TValue>>? deliveryHandler = null) { }
        public Task<DeliveryResult<TKey, TValue>> ProduceAsync(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
            => Task.FromResult(new DeliveryResult<TKey, TValue>());
        public Task<DeliveryResult<TKey, TValue>> ProduceAsync(TopicPartition topicPartition, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
            => Task.FromResult(new DeliveryResult<TKey, TValue>());
    }

    #endregion
}
