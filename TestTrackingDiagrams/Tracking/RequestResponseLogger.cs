using System.Collections.Concurrent;

namespace TestTrackingDiagrams.Tracking;

public static class RequestResponseLogger
{
    private static readonly ConcurrentQueue<RequestResponseLog> RequestsAndResponses = new();

    public static void Log(RequestResponseLog log) => RequestsAndResponses.Enqueue(log);
    public static RequestResponseLog[] RequestAndResponseLogs => RequestsAndResponses.ToArray();
}