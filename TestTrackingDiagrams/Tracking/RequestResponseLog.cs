using System.Net;

namespace TestTrackingDiagrams.Tracking;

public record RequestResponseLog(
    string TestName,
    string TestId,
    OneOf<HttpMethod, string> Method,
    string? Content,
    Uri Uri,
    (string Key, string? Value)[] Headers,
    string ServiceName,
    string CallerName,
    RequestResponseType Type,
    Guid TraceId,
    Guid RequestResponseId,
    bool TrackingIgnore,
    OneOf<HttpStatusCode, string>? StatusCode = null,
    RequestResponseMetaType MetaType = default,
    string? PlantUml = null,
    string? StepName = null,
    string? ParentStepName = null);

public enum RequestResponseType
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