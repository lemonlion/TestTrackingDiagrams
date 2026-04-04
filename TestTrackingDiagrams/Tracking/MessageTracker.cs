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
public class MessageTracker
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _callingServiceName;
    private readonly JsonSerializerOptions _serializerOptions;

    public MessageTracker(
        IHttpContextAccessor httpContextAccessor,
        string callingServiceName,
        JsonSerializerOptions? serializerOptions = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _callingServiceName = callingServiceName;
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions();
    }

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
        var requestResponseId = Guid.NewGuid();
        var content = JsonSerializer.Serialize(payload, _serializerOptions);
        var testInfo = GetTestInfo();

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.TestName,
            testInfo.TestId,
            protocol,
            content,
            destinationUri,
            [],
            destinationName,
            _callingServiceName,
            RequestResponseType.Request,
            testInfo.TraceId,
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
        var content = responsePayload is not null
            ? JsonSerializer.Serialize(responsePayload, _serializerOptions)
            : string.Empty;
        var testInfo = GetTestInfo();

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.TestName,
            testInfo.TestId,
            protocol,
            content,
            destinationUri,
            [],
            destinationName,
            _callingServiceName,
            RequestResponseType.Response,
            testInfo.TraceId,
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

    private (string TestName, string TestId, Guid TraceId) GetTestInfo()
    {
        var headers = _httpContextAccessor.HttpContext!.Request.Headers;

        headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestNameHeader, out var testNameValues);
        headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestIdHeader, out var testIdValues);
        headers.TryGetValue(TestTrackingHttpHeaders.TraceIdHeader, out var traceIdValues);

        return (
            testNameValues.First()!,
            testIdValues.First()!,
            Guid.Parse(traceIdValues.First()!)
        );
    }
}
