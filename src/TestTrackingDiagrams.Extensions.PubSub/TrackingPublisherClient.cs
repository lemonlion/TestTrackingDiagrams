using Google.Cloud.PubSub.V1;
using Google.Protobuf;

namespace TestTrackingDiagrams.Extensions.PubSub;

public class TrackingPublisherClient
{
    private readonly PublisherClient _inner;
    private readonly PubSubTracker _tracker;
    private readonly PubSubTrackingOptions _options;

    public TrackingPublisherClient(PublisherClient inner, PubSubTrackingOptions options)
    {
        _inner = inner;
        _tracker = new PubSubTracker(options);
        _options = options;
    }

    public TopicName TopicName => _inner.TopicName;

    public async Task<string> PublishAsync(PubsubMessage message)
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

    public async Task<string> PublishAsync(string text)
    {
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(text) };
        return await PublishAsync(message);
    }

    public async Task<string> PublishAsync(ByteString data)
    {
        var message = new PubsubMessage { Data = data };
        return await PublishAsync(message);
    }

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

    public Task ShutdownAsync(TimeSpan timeout) => _inner.ShutdownAsync(timeout);
    public PublisherClient Inner => _inner;
}
