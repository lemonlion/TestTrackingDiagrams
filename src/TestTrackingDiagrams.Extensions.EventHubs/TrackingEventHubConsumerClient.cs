using System.Runtime.CompilerServices;
using Azure.Messaging.EventHubs.Consumer;

namespace TestTrackingDiagrams.Extensions.EventHubs;

public class TrackingEventHubConsumerClient
{
    private readonly EventHubConsumerClient _inner;
    private readonly EventHubsTracker _tracker;
    private readonly EventHubsTrackingOptions _options;

    public TrackingEventHubConsumerClient(
        EventHubConsumerClient inner, EventHubsTrackingOptions options)
    {
        _inner = inner;
        _tracker = new EventHubsTracker(options, options.HttpContextAccessor);
        _options = options;
    }

    public string EventHubName => _inner.EventHubName;
    public string ConsumerGroup => _inner.ConsumerGroup;

    public async IAsyncEnumerable<PartitionEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var op = EventHubsOperationClassifier.Classify(
            "ReadEventsAsync", EventHubName);
        var (reqId, traceId) = _tracker.LogRequest(op, null);

        var eventCount = 0;
        await foreach (var partitionEvent in _inner.ReadEventsAsync(cancellationToken))
        {
            eventCount++;
            yield return partitionEvent;
        }

        _tracker.LogResponse(op, reqId, traceId,
            _options.Verbosity == EventHubsTrackingVerbosity.Raw ? $"Events: {eventCount}" : null);
    }

    public async IAsyncEnumerable<PartitionEvent> ReadEventsFromPartitionAsync(
        string partitionId, EventPosition startingPosition,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var op = EventHubsOperationClassifier.Classify(
            "ReadEventsFromPartitionAsync", EventHubName, partitionId);
        var (reqId, traceId) = _tracker.LogRequest(op, null);

        await foreach (var partitionEvent in _inner.ReadEventsFromPartitionAsync(
            partitionId, startingPosition, cancellationToken))
        {
            yield return partitionEvent;
        }

        _tracker.LogResponse(op, reqId, traceId, null);
    }

    public async Task<string[]> GetPartitionIdsAsync(CancellationToken cancellationToken = default) =>
        await _inner.GetPartitionIdsAsync(cancellationToken);

    public async Task CloseAsync(CancellationToken cancellationToken = default) =>
        await _inner.CloseAsync(cancellationToken);

    public EventHubConsumerClient Inner => _inner;
}
