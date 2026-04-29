using System.Collections.Concurrent;
using System.Net;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Central static store for all tracked request/response log entries.
/// All tracking extensions ultimately write to this static collection, which is consumed
/// by report generators to produce sequence diagrams and other visualisations.
/// </summary>
public static class RequestResponseLogger
{
    private static readonly ConcurrentQueue<RequestResponseLog> RequestsAndResponses = new();

    /// <summary>
    /// When set, content longer than this value is truncated at capture time.
    /// The truncated content includes a marker showing the original size.
    /// Default is <c>null</c> (no limit).
    /// </summary>
    public static int? MaxContentLength { get; set; }

    public static void Log(RequestResponseLog log)
    {
        if (MaxContentLength is { } max && log.Content is { Length: var len } && len > max)
            log = log with { Content = $"{log.Content[..max]}\n\n…truncated ({len} chars total)" };

        RequestsAndResponses.Enqueue(log);
    }    public static RequestResponseLog[] RequestAndResponseLogs => RequestsAndResponses.ToArray();
    public static void Clear() => RequestsAndResponses.Clear();

    /// <summary>
    /// Logs a matched request/response pair sharing the same TraceId and RequestResponseId.
    /// Useful for recording interactions that are not captured by the HTTP pipeline
    /// (e.g. in-process calls, Cosmos, Redis, MediatR, blob storage).
    /// </summary>
    public static void LogPair(
        string testName,
        string testId,
        OneOf<HttpMethod, string> method,
        Uri uri,
        string serviceName,
        string callerName,
        string? requestContent = null,
        string? responseContent = null,
        HttpStatusCode? statusCode = null,
        TestPhase phase = TestPhase.Unknown)
    {
        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Log(new RequestResponseLog(testName, testId, method, requestContent, uri,
            [], serviceName, callerName, RequestResponseType.Request, traceId, requestResponseId, false)
        {
            Timestamp = now,
            Phase = phase
        });

        Log(new RequestResponseLog(testName, testId, method, responseContent, uri,
            [], serviceName, callerName, RequestResponseType.Response, traceId, requestResponseId, false,
            statusCode is not null ? (OneOf<HttpStatusCode, string>)statusCode.Value : null)
        {
            Timestamp = now,
            Phase = phase
        });
    }
}