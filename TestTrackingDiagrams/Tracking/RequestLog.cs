namespace TestTrackingDiagrams.Tracking;

public record RequestLog(
    HttpMethod Method,
    string? Content,
    Uri Uri,
    (string Key, string? Value)[] Headers,
    string ServiceName);