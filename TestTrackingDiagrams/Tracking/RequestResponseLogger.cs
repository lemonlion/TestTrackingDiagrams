using System.Collections.Concurrent;

namespace TestTrackingDiagrams.Tracking;

public static class RequestResponseLogger
{
    private static readonly ConcurrentBag<RequestResponseLog> RequestsAndResponses = new();

    public static void Log(RequestResponseLog log) => RequestsAndResponses.Add(log);
    public static RequestResponseLog[] RequestAndResponseLogs => RequestsAndResponses.ToArray();
}