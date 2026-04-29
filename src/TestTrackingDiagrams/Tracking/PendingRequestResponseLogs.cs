using System.Collections.Concurrent;
using System.Net;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// A log entry queued for deferred flushing. Used with <see cref="DeferredLogFlushHandler"/>
/// when request/response logging must be delayed until test identity is available.
/// </summary>
public record PendingLogEntry(
    string ServiceName,
    string CallerName,
    OneOf<HttpMethod, string> Method,
    string? RequestContent,
    string? ResponseContent,
    Uri Uri,
    HttpStatusCode StatusCode = HttpStatusCode.OK,
    string? ActivityTraceId = null,
    string? ActivitySpanId = null)
{
    internal DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Thread-safe queue for deferring request/response log entries until test identity is available.
/// Used with <see cref="DeferredLogFlushHandler"/>.
/// </summary>
public static class PendingRequestResponseLogs
{
    private static readonly ConcurrentQueue<PendingLogEntry> Pending = new();

    public static void Enqueue(PendingLogEntry entry) => Pending.Enqueue(entry);

    public static int Count => Pending.Count;

    public static void FlushAll(string testName, string testId)
    {
        while (Pending.TryDequeue(out var entry))
        {
            var traceId = Guid.NewGuid();
            var requestResponseId = Guid.NewGuid();

            RequestResponseLogger.Log(new RequestResponseLog(
                testName, testId, entry.Method, entry.RequestContent, entry.Uri,
                [], entry.ServiceName, entry.CallerName, RequestResponseType.Request,
                traceId, requestResponseId, false)
            {
                Timestamp = entry.Timestamp,
                ActivityTraceId = entry.ActivityTraceId,
                ActivitySpanId = entry.ActivitySpanId
            });

            RequestResponseLogger.Log(new RequestResponseLog(
                testName, testId, entry.Method, entry.ResponseContent, entry.Uri,
                [], entry.ServiceName, entry.CallerName, RequestResponseType.Response,
                traceId, requestResponseId, false,
                (OneOf<HttpStatusCode, string>)entry.StatusCode)
            {
                Timestamp = entry.Timestamp,
                ActivityTraceId = entry.ActivityTraceId,
                ActivitySpanId = entry.ActivitySpanId
            });
        }
    }

    public static void Clear()
    {
        while (Pending.TryDequeue(out _)) { }
    }
}
