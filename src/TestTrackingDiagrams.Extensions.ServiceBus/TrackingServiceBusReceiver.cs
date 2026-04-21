using Azure.Messaging.ServiceBus;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.ServiceBus;

public class TrackingServiceBusReceiver
{
    private readonly ServiceBusReceiver _inner;
    private readonly ServiceBusTracker _tracker;
    private readonly ServiceBusTrackingOptions _options;

    public TrackingServiceBusReceiver(
        ServiceBusReceiver inner, ServiceBusTracker tracker, ServiceBusTrackingOptions options)
    {
        _inner = inner;
        _tracker = tracker;
        _options = options;
    }

    public string EntityPath => _inner.EntityPath;
    public string FullyQualifiedNamespace => _inner.FullyQualifiedNamespace;
    public string Identifier => _inner.Identifier;
    public bool IsClosed => _inner.IsClosed;
    public ServiceBusReceiveMode ReceiveMode => _inner.ReceiveMode;
    public int PrefetchCount => _inner.PrefetchCount;

    public async Task<ServiceBusReceivedMessage?> ReceiveMessageAsync(
        TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
    {
        var op = ServiceBusOperationClassifier.Classify("ReceiveMessageAsync", EntityPath, null);
        var (reqId, traceId) = _tracker.LogRequest(op, null);
        try
        {
            var result = await _inner.ReceiveMessageAsync(maxWaitTime, cancellationToken);
            var content = result is not null ? GetReceivedMessageContent(result) : null;
            _tracker.LogResponse(op, reqId, traceId, content);
            return result;
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveMessagesAsync(
        int maxMessages, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
    {
        var op = ServiceBusOperationClassifier.Classify("ReceiveMessagesAsync", EntityPath, null);
        var (reqId, traceId) = _tracker.LogRequest(op, null);
        try
        {
            var result = await _inner.ReceiveMessagesAsync(maxMessages, maxWaitTime, cancellationToken);
            var content = _options.Verbosity != ServiceBusTrackingVerbosity.Summarised
                ? $"[{result.Count} messages received]"
                : null;
            _tracker.LogResponse(op, reqId, traceId, content);
            return result;
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task<ServiceBusReceivedMessage?> PeekMessageAsync(
        long? fromSequenceNumber = null, CancellationToken cancellationToken = default)
    {
        var op = ServiceBusOperationClassifier.Classify("PeekMessageAsync", EntityPath, null);
        var (reqId, traceId) = _tracker.LogRequest(op, null);
        try
        {
            var result = await _inner.PeekMessageAsync(fromSequenceNumber, cancellationToken);
            var content = result is not null ? GetReceivedMessageContent(result) : null;
            _tracker.LogResponse(op, reqId, traceId, content);
            return result;
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task CompleteMessageAsync(
        ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
    {
        var op = ServiceBusOperationClassifier.Classify("CompleteMessageAsync", EntityPath, null);
        var (reqId, traceId) = _tracker.LogRequest(op, null);
        try
        {
            await _inner.CompleteMessageAsync(message, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, null);
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task AbandonMessageAsync(
        ServiceBusReceivedMessage message,
        IDictionary<string, object>? propertiesToModify = null,
        CancellationToken cancellationToken = default)
    {
        var op = ServiceBusOperationClassifier.Classify("AbandonMessageAsync", EntityPath, null);
        var (reqId, traceId) = _tracker.LogRequest(op, null);
        try
        {
            await _inner.AbandonMessageAsync(message, propertiesToModify, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, null);
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task DeadLetterMessageAsync(
        ServiceBusReceivedMessage message,
        string? deadLetterReason = null,
        string? deadLetterErrorDescription = null,
        CancellationToken cancellationToken = default)
    {
        var op = ServiceBusOperationClassifier.Classify("DeadLetterMessageAsync", EntityPath, null);
        var content = deadLetterReason is not null && _options.Verbosity != ServiceBusTrackingVerbosity.Summarised
            ? $"Reason: {deadLetterReason}"
            : null;
        var (reqId, traceId) = _tracker.LogRequest(op, content);
        try
        {
            await _inner.DeadLetterMessageAsync(message, deadLetterReason, deadLetterErrorDescription, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, null);
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task DeferMessageAsync(
        ServiceBusReceivedMessage message,
        IDictionary<string, object>? propertiesToModify = null,
        CancellationToken cancellationToken = default)
    {
        var op = ServiceBusOperationClassifier.Classify("DeferMessageAsync", EntityPath, null);
        var (reqId, traceId) = _tracker.LogRequest(op, null);
        try
        {
            await _inner.DeferMessageAsync(message, propertiesToModify, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, null);
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task RenewMessageLockAsync(
        ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
    {
        var op = ServiceBusOperationClassifier.Classify("RenewMessageLockAsync", EntityPath, null);
        var (reqId, traceId) = _tracker.LogRequest(op, null);
        try
        {
            await _inner.RenewMessageLockAsync(message, cancellationToken);
            _tracker.LogResponse(op, reqId, traceId, null);
        }
        catch (Exception ex)
        {
            _tracker.LogResponse(op, reqId, traceId, ex.Message);
            throw;
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
        => await _inner.CloseAsync(cancellationToken);

    public ServiceBusReceiver Inner => _inner;

    private string? GetReceivedMessageContent(ServiceBusReceivedMessage message)
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
