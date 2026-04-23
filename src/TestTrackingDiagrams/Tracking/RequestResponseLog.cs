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
    string? DependencyCategory = null)
{
    public bool IsOverrideStart { get; set; }
    public bool IsOverrideEnd { get; set; }
    public bool IsActionStart { get; set; }
    public string? PlantUml { get; set; }
    public string[]? FocusFields { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public string? ActivitySpanId { get; set; }
    public string? ActivityTraceId { get; set; }
    public TestPhase Phase { get; set; }
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