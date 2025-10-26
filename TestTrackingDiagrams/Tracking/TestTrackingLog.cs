using System.Net;

namespace TestTrackingDiagrams.Tracking;

public record TestTrackingLog(
    string TestName,
    string TestId,
    OneOf<HttpMethod, string> Method,
    string? Content,
    Uri Uri,
    (string Key, string? Value)[] Headers,
    string ServiceName,
    string CallerName,
    TestTrackingLogType Type,
    Guid TraceId,
    Guid RequestResponseId,
    bool TrackingIgnore,
    OneOf<HttpStatusCode, string>? StatusCode = null,
    RequestResponseMetaType MetaType = default,
    string? plantUml = null);

public enum TestTrackingLogType
{
    Request,
    Response,
    OverrideStart,
    OverrideEnd,
    ActionStart,
    ActionEnd
}

public enum RequestResponseMetaType
{
    Default,
    Event
}