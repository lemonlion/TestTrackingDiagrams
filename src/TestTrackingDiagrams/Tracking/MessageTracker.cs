using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Constants;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Logs non-HTTP interactions (events, messages, commands) to the
/// <see cref="RequestResponseLogger"/> so they appear in the PlantUML
/// sequence diagrams alongside regular HTTP traffic.
///
/// Register an instance in DI (typically as a singleton) and inject it
/// into any fake or stub that simulates publishing/sending messages.
/// </summary>
public class MessageTracker : ITrackingComponent
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly string _callingServiceName;
    private readonly string _serviceName;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly Func<(string Name, string Id)>? _testInfoFallback;
    private readonly MessageTrackerVerbosity _verbosity;
    private int _invocationCount;

    /// <summary>
    /// Creates a <see cref="MessageTracker"/> using an options record.
    /// This is the recommended constructor — it follows the same pattern
    /// as all other TTD extensions.
    /// </summary>
    public MessageTracker(MessageTrackerOptions options)
    {
        _httpContextAccessor = null;
        _callingServiceName = options.CallingServiceName;
        _serviceName = options.ServiceName;
        _serializerOptions = options.SerializerOptions ?? new JsonSerializerOptions();
        _testInfoFallback = options.CurrentTestInfoFetcher;
        _verbosity = options.Verbosity;
        TrackingComponentRegistry.Register(this);
    }

    /// <summary>
    /// Legacy constructor that reads test info from <see cref="IHttpContextAccessor"/>
    /// request headers, with an optional fallback delegate.
    /// Kept for backward compatibility — prefer the <see cref="MessageTrackerOptions"/>
    /// overload for new code.
    /// </summary>
    public MessageTracker(
        IHttpContextAccessor httpContextAccessor,
        string callingServiceName,
        JsonSerializerOptions? serializerOptions = null,
        Func<(string Name, string Id)>? testInfoFallback = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _callingServiceName = callingServiceName;
        _serviceName = "MessageBus";
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions();
        _testInfoFallback = testInfoFallback;
        _verbosity = MessageTrackerVerbosity.Detailed;
        TrackingComponentRegistry.Register(this);
    }

    // ─── ITrackingComponent ─────────────────────────────────────

    public string ComponentName => $"MessageTracker ({_serviceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    /// <summary>
    /// Logs a request entry for a message being sent to a named destination.
    /// Returns a correlation ID to pass to <see cref="TrackMessageResponse"/>.
    /// </summary>
    /// <param name="protocol">The protocol or transport label shown in the diagram (e.g. "Kafka", "EventGrid", "SNS", "RabbitMQ").</param>
    /// <param name="destinationName">The name of the destination service or topic shown in the diagram.</param>
    /// <param name="destinationUri">A URI representing the destination (e.g. <c>new Uri("kafka://orders-topic")</c>).</param>
    /// <param name="payload">The message payload. Will be JSON-serialised and shown in the diagram note.</param>
    /// <returns>A correlation ID that must be passed to <see cref="TrackMessageResponse"/> to pair the request and response.</returns>
    public Guid TrackMessageRequest(string protocol, string destinationName, Uri destinationUri, object payload)
    {
        Interlocked.Increment(ref _invocationCount);

        var testInfo = GetTestInfo();
        if (testInfo is null)
            return Guid.Empty;

        var requestResponseId = Guid.NewGuid();
        var content = _verbosity == MessageTrackerVerbosity.Summarised
            ? null
            : JsonSerializer.Serialize(payload, _serializerOptions);

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.TestName,
            testInfo.Value.TestId,
            protocol,
            content,
            destinationUri,
            [],
            destinationName,
            _callingServiceName,
            RequestResponseType.Request,
            testInfo.Value.TraceId,
            requestResponseId,
            false,
            MetaType: RequestResponseMetaType.Event
        )
        {
            Timestamp = DateTimeOffset.UtcNow,
            ActivitySpanId = Activity.Current?.SpanId.ToString(),
            ActivityTraceId = Activity.Current?.TraceId.ToString()
        });

        return requestResponseId;
    }

    /// <summary>
    /// Logs a response entry for a message that was sent.
    /// </summary>
    /// <param name="protocol">The protocol or transport label (must match the value passed to <see cref="TrackMessageRequest"/>).</param>
    /// <param name="destinationName">The destination service or topic name (must match the value passed to <see cref="TrackMessageRequest"/>).</param>
    /// <param name="destinationUri">The destination URI (must match the value passed to <see cref="TrackMessageRequest"/>).</param>
    /// <param name="requestResponseId">The correlation ID returned by <see cref="TrackMessageRequest"/>.</param>
    /// <param name="responsePayload">Optional response payload (e.g. an acknowledgement). Will be JSON-serialised if provided.</param>
    public void TrackMessageResponse(string protocol, string destinationName, Uri destinationUri, Guid requestResponseId, object? responsePayload = null)
    {
        var testInfo = GetTestInfo();
        if (testInfo is null)
            return;

        var content = _verbosity == MessageTrackerVerbosity.Summarised
            ? null
            : responsePayload is not null
                ? JsonSerializer.Serialize(responsePayload, _serializerOptions)
                : string.Empty;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.TestName,
            testInfo.Value.TestId,
            protocol,
            content,
            destinationUri,
            [],
            destinationName,
            _callingServiceName,
            RequestResponseType.Response,
            testInfo.Value.TraceId,
            requestResponseId,
            false,
            "Responded",
            MetaType: RequestResponseMetaType.Event
        )
        {
            Timestamp = DateTimeOffset.UtcNow,
            ActivitySpanId = Activity.Current?.SpanId.ToString(),
            ActivityTraceId = Activity.Current?.TraceId.ToString()
        });
    }

    /// <summary>
    /// Logs a complete send event as a fire-and-forget pair (request + response)
    /// in a single call. Ideal for event-driven patterns where there is no
    /// meaningful response to track separately.
    /// </summary>
    /// <param name="protocol">The protocol or transport label shown in the diagram.</param>
    /// <param name="destinationName">The name of the destination service or topic shown in the diagram.</param>
    /// <param name="destinationUri">A URI representing the destination.</param>
    /// <param name="payload">Optional message payload. Will be JSON-serialised when verbosity allows.</param>
    public void TrackSendEvent(string protocol, string destinationName, Uri destinationUri, object? payload = null)
    {
        var id = TrackMessageRequest(protocol, destinationName, destinationUri, payload ?? new { });
        if (id != Guid.Empty)
            TrackMessageResponse(protocol, destinationName, destinationUri, id);
    }

    private (string TestName, string TestId, Guid TraceId)? GetTestInfo()
    {
        var context = _httpContextAccessor?.HttpContext;
        if (context is not null)
        {
            var headers = context.Request.Headers;

            headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestNameHeader, out var testNameValues);
            headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestIdHeader, out var testIdValues);
            headers.TryGetValue(TestTrackingHttpHeaders.TraceIdHeader, out var traceIdValues);

            var hasHeaders = testNameValues.Count > 0 && testIdValues.Count > 0 && traceIdValues.Count > 0;
            if (hasHeaders)
            {
                return (
                    testNameValues.First()!,
                    testIdValues.First()!,
                    Guid.Parse(traceIdValues.First()!)
                );
            }
        }

        if (_testInfoFallback is not null)
        {
            var info = _testInfoFallback();
            return (info.Name, info.Id, Guid.NewGuid());
        }

        return null;
    }
}
