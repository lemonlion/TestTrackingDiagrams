using System.Net;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Represents a captured HTTP response received from the server.
/// Used by <see cref="TestTrackingMessageHandler"/> to log incoming responses.
/// </summary>
public record ResponseLog(
    HttpStatusCode StatusCode,
    string? Content,
    (string Key, string? Value)[] Headers);