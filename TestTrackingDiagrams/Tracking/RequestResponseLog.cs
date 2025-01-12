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
    RequestResponseMetaType MetaType = default)
{
    public bool IsOverrideSummary { get; set; }
    public bool IsOverrideEnding { get; set; }
    public string? PlantUml { get; set; }
};

public enum RequestResponseType
{
    Request,
    Response
}

public enum RequestResponseMetaType
{
    Default,
    Event
}