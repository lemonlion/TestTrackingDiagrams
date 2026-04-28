using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;

namespace TestTrackingDiagrams.Extensions.PubSub;

/// <summary>
/// A <see cref="PublisherClient"/> subclass that intercepts publish operations
/// for test diagram tracking.
/// </summary>
public class TrackingPublisherClient : PublisherClient
{
    private readonly PublisherClient _inner;
    private readonly PubSubTracker _tracker;
    private readonly PubSubTrackingOptions _options;

    public TrackingPublisherClient(
        PublisherClient inner, PubSubTrackingOptions options,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _inner = inner;
        _tracker = new PubSubTracker(options, httpContextAccessor ?? options.HttpContextAccessor);
        _options = options;
    }

    /// <summary>The underlying real <see cref="PublisherClient"/>.</summary>
    public PublisherClient Inner => _inner;

    public override TopicName TopicName => _inner.TopicName;

    public override async Task<string> PublishAsync(PubsubMessage message)
    {
        var op = PubSubOperationClassifier.Classify(
            "PublishAsync", _inner.TopicName?.ToString(), null, 1);
        var content = _options.Verbosity != PubSubTrackingVerbosity.Summarised
            ? message.Data?.ToStringUtf8() : null;
        var (reqId, traceId) = _tracker.LogRequest(op, content);

        try
        {
            var messageId = await _inner.PublishAsync(message);
            _tracker.LogResponse(op, reqId, traceId,
                _options.Verbosity == PubSubTrackingVerbosity.Raw ? $"MessageId: {messageId}" : null);
            return messageId;
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Publishes a batch of messages individually with aggregate tracking.
    /// </summary>
    public async Task<IReadOnlyList<string>> PublishAsync(IEnumerable<PubsubMessage> messages)
    {
        var messageList = messages.ToList();
        var op = PubSubOperationClassifier.Classify(
            "PublishAsync", _inner.TopicName?.ToString(), null, messageList.Count);
        var content = _options.Verbosity == PubSubTrackingVerbosity.Raw
            ? string.Join(", ", messageList.Select(m => m.Data?.ToStringUtf8() ?? ""))
            : null;
        var (reqId, traceId) = _tracker.LogRequest(op, content);

        try
        {
            var ids = new List<string>();
            foreach (var msg in messageList)
                ids.Add(await _inner.PublishAsync(msg));

            _tracker.LogResponse(op, reqId, traceId, null);
            return ids;
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public override Task ShutdownAsync(TimeSpan timeout) => _inner.ShutdownAsync(timeout);
    public override Task ShutdownAsync(CancellationToken cancellationToken) => _inner.ShutdownAsync(cancellationToken);
    public override ValueTask DisposeAsync() => _inner.DisposeAsync();
    public override void ResumePublish(string orderingKey) => _inner.ResumePublish(orderingKey);
}
