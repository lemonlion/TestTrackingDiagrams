using System.Net;

namespace TestTrackingDiagrams.Tracking;

public record RequestResponseLog(
    string TestName,
    Guid TestId,
    HttpMethod Method,
    string? Content,
    Uri Uri,
    (string Key, string? Value)[] Headers,
    string ServiceName,
    string CallerName,
    RequestResponseType Type,
    Guid TraceId,
    Guid RequestResponseId,
    bool TrackingIgnore,
    HttpStatusCode? StatusCode = null);

public enum RequestResponseType
{
    Request,
    Response
}