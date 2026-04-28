using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Http;

namespace TestTrackingDiagrams.Extensions.PubSub;

/// <summary>
/// A <see cref="SubscriberClient"/> subclass that intercepts message processing
/// for test diagram tracking.
/// </summary>
public class TrackingSubscriberClient : SubscriberClient
{
    private readonly SubscriberClient _inner;
    private readonly PubSubTracker _tracker;
    private readonly PubSubTrackingOptions _options;

    public TrackingSubscriberClient(
        SubscriberClient inner, PubSubTrackingOptions options,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _inner = inner;
        _tracker = new PubSubTracker(options, httpContextAccessor ?? options.HttpContextAccessor);
        _options = options;
    }

    /// <summary>The underlying real <see cref="SubscriberClient"/>.</summary>
    public SubscriberClient Inner => _inner;

    public override SubscriptionName SubscriptionName => _inner.SubscriptionName;

    public override Task StartAsync(Func<PubsubMessage, CancellationToken, Task<Reply>> messageHandler)
    {
        async Task<Reply> wrappedHandler(PubsubMessage msg, CancellationToken ct)
        {
            var op = PubSubOperationClassifier.Classify(
                "Receive", null, _inner.SubscriptionName?.ToString(), 1);
            var content = _options.Verbosity != PubSubTrackingVerbosity.Summarised
                ? msg.Data?.ToStringUtf8() : null;
            var (reqId, traceId) = _tracker.LogRequest(op, content);

            var reply = await messageHandler(msg, ct);

            var replyLabel = reply == Reply.Ack ? "Ack" : "Nack";
            _tracker.LogResponse(op, reqId, traceId,
                _options.Verbosity == PubSubTrackingVerbosity.Raw ? replyLabel : null);

            return reply;
        }

        return _inner.StartAsync(wrappedHandler);
    }

    public override Task StopAsync(CancellationToken cancellationToken) => _inner.StopAsync(cancellationToken);
    public override ValueTask DisposeAsync() => _inner.DisposeAsync();
}
