using Google.Cloud.PubSub.V1;

namespace TestTrackingDiagrams.Extensions.PubSub;

public class TrackingSubscriberClient
{
    private readonly SubscriberClient _inner;
    private readonly PubSubTracker _tracker;
    private readonly PubSubTrackingOptions _options;

    public TrackingSubscriberClient(SubscriberClient inner, PubSubTrackingOptions options)
    {
        _inner = inner;
        _tracker = new PubSubTracker(options);
        _options = options;
    }

    public SubscriptionName SubscriptionName => _inner.SubscriptionName;

    public Task StartAsync(Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> messageHandler)
    {
        async Task<SubscriberClient.Reply> wrappedHandler(PubsubMessage msg, CancellationToken ct)
        {
            var op = PubSubOperationClassifier.Classify(
                "Receive", null, _inner.SubscriptionName?.ToString(), 1);
            var content = _options.Verbosity != PubSubTrackingVerbosity.Summarised
                ? msg.Data?.ToStringUtf8() : null;
            var (reqId, traceId) = _tracker.LogRequest(op, content);

            var reply = await messageHandler(msg, ct);

            var replyLabel = reply == SubscriberClient.Reply.Ack ? "Ack" : "Nack";
            _tracker.LogResponse(op, reqId, traceId,
                _options.Verbosity == PubSubTrackingVerbosity.Raw ? replyLabel : null);

            return reply;
        }

        return _inner.StartAsync(wrappedHandler);
    }

    public Task StopAsync(CancellationToken ct = default) => _inner.StopAsync(ct);
    public SubscriberClient Inner => _inner;
}
