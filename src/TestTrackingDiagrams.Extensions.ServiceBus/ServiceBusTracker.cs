using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.ServiceBus;

public class ServiceBusTracker : ITrackingComponent
{
    private readonly ServiceBusTrackingOptions _options;
    private int _invocationCount;

    public ServiceBusTracker(ServiceBusTrackingOptions options)
    {
        _options = options;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"ServiceBusTracker ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    public (Guid RequestResponseId, Guid TraceId) LogRequest(
        ServiceBusOperationInfo operation, string? content)
    {
        Interlocked.Increment(ref _invocationCount);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return (Guid.Empty, Guid.Empty);

        var requestResponseId = Guid.NewGuid();
        var traceId = Guid.NewGuid();

        var label = ServiceBusOperationClassifier.GetDiagramLabel(operation, _options.Verbosity);
        var uri = BuildUri(operation);

        OneOf<System.Net.Http.HttpMethod, string> method = label;

        var metaType = operation.Operation is
            ServiceBusOperation.Send or ServiceBusOperation.SendBatch or
            ServiceBusOperation.Receive or ServiceBusOperation.ReceiveBatch or
            ServiceBusOperation.Schedule or ServiceBusOperation.Peek
            ? RequestResponseMetaType.Event
            : RequestResponseMetaType.Default;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name,
            testInfo.Value.Id,
            method,
            _options.Verbosity == ServiceBusTrackingVerbosity.Summarised ? null : content,
            uri,
            [],
            _options.ServiceName,
            _options.CallingServiceName,
            RequestResponseType.Request,
            traceId,
            requestResponseId,
            false,
            MetaType: metaType,
            DependencyCategory: "ServiceBus"
        ));

        return (requestResponseId, traceId);
    }

    public void LogResponse(
        ServiceBusOperationInfo operation,
        Guid requestResponseId, Guid traceId, string? content)
    {
        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return;

        if (requestResponseId == Guid.Empty)
            return;

        var label = ServiceBusOperationClassifier.GetDiagramLabel(operation, _options.Verbosity);
        var uri = BuildUri(operation);

        OneOf<System.Net.Http.HttpMethod, string> method = label;

        var metaType = operation.Operation is
            ServiceBusOperation.Send or ServiceBusOperation.SendBatch or
            ServiceBusOperation.Receive or ServiceBusOperation.ReceiveBatch or
            ServiceBusOperation.Schedule or ServiceBusOperation.Peek
            ? RequestResponseMetaType.Event
            : RequestResponseMetaType.Default;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name,
            testInfo.Value.Id,
            method,
            _options.Verbosity == ServiceBusTrackingVerbosity.Summarised ? null : content,
            uri,
            [],
            _options.ServiceName,
            _options.CallingServiceName,
            RequestResponseType.Response,
            traceId,
            requestResponseId,
            false,
            MetaType: metaType,
            DependencyCategory: "ServiceBus"
        ));
    }

    private Uri BuildUri(ServiceBusOperationInfo op)
    {
        var queue = op.QueueOrTopicName ?? "unknown";
        var sub = op.SubscriptionName;

        return _options.Verbosity switch
        {
            ServiceBusTrackingVerbosity.Summarised => new Uri($"servicebus://{queue}/"),
            ServiceBusTrackingVerbosity.Detailed => sub is not null
                ? new Uri($"servicebus://{queue}/{sub}")
                : new Uri($"servicebus://{queue}"),
            _ => sub is not null
                ? new Uri($"servicebus://{queue}/{sub}")
                : new Uri($"servicebus://{queue}")
        };
    }
}
