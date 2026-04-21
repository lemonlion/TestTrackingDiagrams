using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Kafka;

public class KafkaTracker : ITrackingComponent
{
    private readonly KafkaTrackingOptions _options;
    private int _invocationCount;

    public KafkaTracker(KafkaTrackingOptions options)
    {
        _options = options;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"KafkaTracker ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    public void LogProduce(KafkaOperationInfo op, string? content)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!_options.TrackProduce) return;
        LogOutgoing(op, content);
    }

    public void LogConsume(KafkaOperationInfo op, string? content)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!_options.TrackConsume) return;
        LogIncoming(op, content);
    }

    public void LogSubscribe(KafkaOperationInfo op)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!_options.TrackSubscribe) return;
        LogOutgoing(op, null);
    }

    public void LogCommit(KafkaOperationInfo op)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!_options.TrackCommit) return;
        LogOutgoing(op, null);
    }

    private void LogOutgoing(KafkaOperationInfo op, string? content)
    {
        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null) return;

        var label = KafkaOperationClassifier.GetDiagramLabel(op, _options.Verbosity);
        var uri = KafkaOperationClassifier.BuildUri(op, _options.Verbosity);

        var body = _options.Verbosity == KafkaTrackingVerbosity.Summarised ? null : content;

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, body, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Request, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event));

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, null, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Response, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event));
    }

    private void LogIncoming(KafkaOperationInfo op, string? content)
    {
        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null) return;

        var label = KafkaOperationClassifier.GetDiagramLabel(op, _options.Verbosity);
        var uri = KafkaOperationClassifier.BuildUri(op, _options.Verbosity);

        var body = _options.Verbosity == KafkaTrackingVerbosity.Summarised ? null : content;

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        // Consume is incoming: swap caller/service
        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, body, uri, [],
            _options.CallingServiceName, _options.ServiceName,
            RequestResponseType.Request, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event));

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, null, uri, [],
            _options.CallingServiceName, _options.ServiceName,
            RequestResponseType.Response, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event));
    }
}
