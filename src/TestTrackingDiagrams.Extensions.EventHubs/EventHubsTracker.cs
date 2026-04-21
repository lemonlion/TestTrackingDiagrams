using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.EventHubs;

public class EventHubsTracker : ITrackingComponent
{
    private readonly EventHubsTrackingOptions _options;
    private int _invocationCount;

    public EventHubsTracker(EventHubsTrackingOptions options)
    {
        _options = options;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"EventHubsTracker ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    public (Guid RequestResponseId, Guid TraceId) LogRequest(
        EventHubsOperationInfo operation, string? content)
    {
        Interlocked.Increment(ref _invocationCount);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null) return (Guid.Empty, Guid.Empty);

        var uri = BuildUri(operation);
        var label = EventHubsOperationClassifier.GetDiagramLabel(operation, _options.Verbosity);
        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        var logContent = _options.Verbosity == EventHubsTrackingVerbosity.Summarised ? null : content;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, logContent, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Request, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event));

        return (requestResponseId, traceId);
    }

    public void LogResponse(
        EventHubsOperationInfo operation,
        Guid requestResponseId, Guid traceId, string? content)
    {
        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null) return;

        var uri = BuildUri(operation);
        var label = EventHubsOperationClassifier.GetDiagramLabel(operation, _options.Verbosity);

        var logContent = _options.Verbosity == EventHubsTrackingVerbosity.Summarised ? null : content;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, logContent, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Response, traceId, requestResponseId, false));
    }

    private Uri BuildUri(EventHubsOperationInfo op)
    {
        var hub = op.EventHubName ?? "unknown";

        return _options.Verbosity switch
        {
            EventHubsTrackingVerbosity.Raw when op.PartitionId is not null =>
                new Uri($"eventhubs:///{hub}/{op.PartitionId}"),
            EventHubsTrackingVerbosity.Detailed when op.PartitionId is not null =>
                new Uri($"eventhubs:///{hub}/{op.PartitionId}"),
            _ => new Uri($"eventhubs:///{hub}")
        };
    }
}
