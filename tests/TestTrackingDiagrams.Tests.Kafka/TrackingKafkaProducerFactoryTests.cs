using Confluent.Kafka;
using TestTrackingDiagrams.Extensions.Kafka;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Kafka;

public class TrackingKafkaProducerFactoryTests
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
        CallingServiceName = "TestCaller",
        CurrentTestInfoFetcher = () => ("Factory Test", _testId),
    };

    // ─── TrackingKafkaProducerFactory ───────────────────────

    [Fact]
    public void Create_Returns_TrackingKafkaProducer()
    {
        var options = MakeOptions();
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducerFactory<string, string>();
        var factory = new TrackingKafkaProducerFactory<string, string>(inner, tracker, options);

        var producer = factory.Create(config => { });

        Assert.IsType<TrackingKafkaProducer<string, string>>(producer);
    }

    [Fact]
    public void Create_Delegates_To_Inner_Factory()
    {
        var options = MakeOptions();
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducerFactory<string, string>();
        var factory = new TrackingKafkaProducerFactory<string, string>(inner, tracker, options);

        factory.Create(config => { });

        Assert.True(inner.CreateCalled);
    }

    [Fact]
    public void Create_Passes_Configure_Action_To_Inner()
    {
        var options = MakeOptions();
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducerFactory<string, string>();
        var factory = new TrackingKafkaProducerFactory<string, string>(inner, tracker, options);

        ProducerConfig? capturedConfig = null;
        factory.Create(config =>
        {
            config.ClientId = "test-client";
            capturedConfig = config;
        });

        Assert.NotNull(capturedConfig);
        Assert.Equal("test-client", capturedConfig!.ClientId);
    }

    [Fact]
    public async Task Created_Producer_Tracks_ProduceAsync_Operations()
    {
        var options = MakeOptions();
        var tracker = new KafkaTracker(options);
        var inner = new FakeProducerFactory<string, string>();
        var factory = new TrackingKafkaProducerFactory<string, string>(inner, tracker, options);

        var producer = factory.Create(config => { });
        await producer.ProduceAsync("test-topic", new Message<string, string> { Key = "k1", Value = "v1" });

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    // ─── KafkaProducerFactory (default) ─────────────────────

    [Fact]
    public void Default_Factory_Implements_Interface()
    {
        var factory = new KafkaProducerFactory<string, string>();
        Assert.IsAssignableFrom<IKafkaProducerFactory<string, string>>(factory);
    }

    #region Test Doubles

    private class FakeProducerFactory<TKey, TValue> : IKafkaProducerFactory<TKey, TValue>
    {
        public bool CreateCalled { get; private set; }

        public IProducer<TKey, TValue> Create(Action<ProducerConfig> configure)
        {
            CreateCalled = true;
            var config = new ProducerConfig();
            configure(config);
            return new FakeProducer<TKey, TValue>();
        }
    }

    private class FakeProducer<TKey, TValue> : IProducer<TKey, TValue>
    {
        public Handle Handle => throw new NotImplementedException();
        public string Name => "fake";
        public void SetSaslCredentials(string username, string password) { }
        public void Dispose() { }
        public int AddBrokers(string brokers) => 0;
        public void InitTransactions(TimeSpan timeout) { }
        public void BeginTransaction() { }
        public void CommitTransaction(TimeSpan timeout) { }
        public void CommitTransaction() { }
        public void AbortTransaction(TimeSpan timeout) { }
        public void AbortTransaction() { }
        public void SendOffsetsToTransaction(IEnumerable<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata, TimeSpan timeout) { }
        public int Flush(TimeSpan timeout) => 0;
        public void Flush(CancellationToken cancellationToken = default) { }
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
