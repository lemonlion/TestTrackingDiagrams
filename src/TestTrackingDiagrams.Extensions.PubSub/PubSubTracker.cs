using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.PubSub;

public class PubSubTracker : ITrackingComponent
{
    private readonly PubSubTrackingOptions _options;
    private int _invocationCount;

    public PubSubTracker(PubSubTrackingOptions options)
    {
        _options = options;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"PubSubTracker ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    public (Guid RequestResponseId, Guid TraceId) LogRequest(
        PubSubOperationInfo operation, string? content)
    {
        Interlocked.Increment(ref _invocationCount);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null) return (Guid.Empty, Guid.Empty);

        var uri = BuildUri(operation);
        var label = PubSubOperationClassifier.GetDiagramLabel(operation, _options.Verbosity);
        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        var logContent = _options.Verbosity == PubSubTrackingVerbosity.Summarised ? null : content;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, logContent, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Request, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event));

        return (requestResponseId, traceId);
    }

    public void LogResponse(
        PubSubOperationInfo operation,
        Guid requestResponseId, Guid traceId, string? content)
    {
        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null) return;

        var uri = BuildUri(operation);
        var label = PubSubOperationClassifier.GetDiagramLabel(operation, _options.Verbosity);

        var logContent = _options.Verbosity == PubSubTrackingVerbosity.Summarised ? null : content;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, logContent, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Response, traceId, requestResponseId, false));
    }

    private Uri BuildUri(PubSubOperationInfo op)
    {
        var topic = ShortName(op.TopicName);
        var sub = ShortName(op.SubscriptionName);
        var name = topic ?? sub ?? "unknown";

        return _options.Verbosity switch
        {
            PubSubTrackingVerbosity.Raw =>
                new Uri($"pubsub:///{op.TopicName ?? op.SubscriptionName ?? "unknown"}"),
            _ =>
                new Uri($"pubsub:///{name}")
        };
    }

    private static string? ShortName(string? fullName) =>
        fullName?.Split('/').LastOrDefault() ?? fullName;
}
