namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Represents a captured HTTP request before it is sent to the server.
/// Used by <see cref="TestTrackingMessageHandler"/> to log outgoing requests.
/// </summary>
public record RequestLog(
    HttpMethod Method,
    string? Content,
    Uri Uri,
    (string Key, string? Value)[] Headers,
    string ServiceName,
    string CallerName);