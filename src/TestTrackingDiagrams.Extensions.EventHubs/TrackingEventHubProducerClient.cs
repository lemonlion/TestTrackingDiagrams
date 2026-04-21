using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace TestTrackingDiagrams.Extensions.EventHubs;

public class TrackingEventHubProducerClient
{
    private readonly EventHubProducerClient _inner;
    private readonly EventHubsTracker _tracker;
    private readonly EventHubsTrackingOptions _options;

    public TrackingEventHubProducerClient(
        EventHubProducerClient inner, EventHubsTrackingOptions options)
    {
        _inner = inner;
        _tracker = new EventHubsTracker(options);
        _options = options;
    }

    public string EventHubName => _inner.EventHubName;
    public string FullyQualifiedNamespace => _inner.FullyQualifiedNamespace;
    public bool IsClosed => _inner.IsClosed;

    public async Task SendAsync(
        IEnumerable<EventData> eventBatch,
        CancellationToken cancellationToken = default)
    {
        var events = eventBatch.ToList();
        var op = EventHubsOperationClassifier.Classify(
            "SendAsync", EventHubName, null, events.Count);
        var content = _options.Verbosity != EventHubsTrackingVerbosity.Summarised
            ? SerializeEvents(events) : null;
        var (reqId, traceId) = _tracker.LogRequest(op, content);

        try
        {
            await _inner.SendAsync(events, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, null);
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task SendAsync(
        IEnumerable<EventData> eventBatch,
        SendEventOptions sendOptions,
        CancellationToken cancellationToken = default)
    {
        var events = eventBatch.ToList();
        var partitionId = sendOptions?.PartitionId;
        var op = EventHubsOperationClassifier.Classify(
            "SendAsync", EventHubName, partitionId, events.Count);
        var content = _options.Verbosity != EventHubsTrackingVerbosity.Summarised
            ? SerializeEvents(events) : null;
        var (reqId, traceId) = _tracker.LogRequest(op, content);

        try
        {
            await _inner.SendAsync(events, sendOptions, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, null);
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task SendAsync(
        EventDataBatch eventBatch,
        CancellationToken cancellationToken = default)
    {
        var op = EventHubsOperationClassifier.Classify(
            "SendAsync", EventHubName, null, eventBatch.Count);
        var (reqId, traceId) = _tracker.LogRequest(op, null);

        try
        {
            await _inner.SendAsync(eventBatch, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, null);
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task<EventDataBatch> CreateBatchAsync(
        CreateBatchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await _inner.CreateBatchAsync(options, cancellationToken);
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default) =>
        await _inner.CloseAsync(cancellationToken);

    public EventHubProducerClient Inner => _inner;

    private static string? SerializeEvents(IList<EventData> events)
    {
        if (events.Count == 0) return null;
        if (events.Count == 1) return events[0].EventBody?.ToString();
        return $"[{string.Join(", ", events.Select(e => e.EventBody?.ToString() ?? "null"))}]";
    }
}
