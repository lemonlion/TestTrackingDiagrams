using Azure.Messaging.ServiceBus;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.ServiceBus;

public class TrackingServiceBusSender
{
    private readonly ServiceBusSender _inner;
    private readonly ServiceBusTracker _tracker;
    private readonly ServiceBusTrackingOptions _options;

    public TrackingServiceBusSender(
        ServiceBusSender inner, ServiceBusTracker tracker, ServiceBusTrackingOptions options)
    {
        _inner = inner;
        _tracker = tracker;
        _options = options;
    }

    public string EntityPath => _inner.EntityPath;
    public string FullyQualifiedNamespace => _inner.FullyQualifiedNamespace;
    public string Identifier => _inner.Identifier;
    public bool IsClosed => _inner.IsClosed;

    public async Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        var op = ServiceBusOperationClassifier.Classify("SendMessageAsync", EntityPath, null);
        var content = GetMessageContent(message);
        var (reqId, traceId) = _tracker.LogRequest(op, content);
        try
        {
            await _inner.SendMessageAsync(message, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, null);
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task SendMessagesAsync(
        IEnumerable<ServiceBusMessage> messages, CancellationToken cancellationToken = default)
    {
        var messageList = messages as IReadOnlyList<ServiceBusMessage> ?? messages.ToList();
        var op = ServiceBusOperationClassifier.Classify("SendMessagesAsync", EntityPath, null) with
        {
            MessageCount = messageList.Count
        };
        var content = _options.Verbosity != ServiceBusTrackingVerbosity.Summarised
            ? $"[{messageList.Count} messages]"
            : null;
        var (reqId, traceId) = _tracker.LogRequest(op, content);
        try
        {
            await _inner.SendMessagesAsync(messageList, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, null);
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task<long> ScheduleMessageAsync(
        ServiceBusMessage message, DateTimeOffset scheduledEnqueueTime, CancellationToken cancellationToken = default)
    {
        var op = ServiceBusOperationClassifier.Classify("ScheduleMessageAsync", EntityPath, null);
        var content = GetMessageContent(message);
        var (reqId, traceId) = _tracker.LogRequest(op, content);
        try
        {
            var result = await _inner.ScheduleMessageAsync(message, scheduledEnqueueTime, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, $"SequenceNumber: {result}");
            return result;
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken = default)
    {
        var op = ServiceBusOperationClassifier.Classify("CancelScheduledMessageAsync", EntityPath, null);
        var (reqId, traceId) = _tracker.LogRequest(op, $"SequenceNumber: {sequenceNumber}");
        try
        {
            await _inner.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, null);
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(CancellationToken cancellationToken = default)
        => await _inner.CreateMessageBatchAsync(cancellationToken);

    public async ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(
        CreateMessageBatchOptions options, CancellationToken cancellationToken = default)
        => await _inner.CreateMessageBatchAsync(options, cancellationToken);

    public async Task CloseAsync(CancellationToken cancellationToken = default)
        => await _inner.CloseAsync(cancellationToken);

    public ServiceBusSender Inner => _inner;

    private string? GetMessageContent(ServiceBusMessage message)
    {
        if (_options.Verbosity == ServiceBusTrackingVerbosity.Summarised)
            return null;

        try
        {
            return message.Body?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
