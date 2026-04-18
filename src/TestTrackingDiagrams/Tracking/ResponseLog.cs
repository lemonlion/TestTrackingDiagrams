using System.Net;

namespace TestTrackingDiagrams.Tracking;

public record ResponseLog(
    HttpStatusCode StatusCode,
    string? Content,
    (string Key, string? Value)[] Headers);