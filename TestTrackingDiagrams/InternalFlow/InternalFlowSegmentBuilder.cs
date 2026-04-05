using System.Diagnostics;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.InternalFlow;

/// <summary>
/// Builds segments of internal flow by correlating OTel spans with
/// <see cref="RequestResponseLog"/> boundary timestamps.
/// </summary>
public static class InternalFlowSegmentBuilder
{
    /// <summary>
    /// Builds a dictionary mapping segment keys (e.g. "iflow-{guid}" and "iflow-{guid}-res")
    /// to the spans that occurred during each segment.
    /// </summary>
    public static Dictionary<string, InternalFlowSegment> BuildSegments(
        RequestResponseLog[] logs,
        Activity[] spans)
    {
        var segments = new Dictionary<string, InternalFlowSegment>();

        if (spans.Length == 0)
            return segments;

        var logsByTest = logs
            .Where(l => l.Timestamp.HasValue)
            .GroupBy(l => l.TestId);

        foreach (var testGroup in logsByTest)
        {
            var orderedLogs = testGroup.OrderBy(l => l.Timestamp!.Value).ToArray();
            var testSpans = FilterSpansByTestTraceIds(spans, orderedLogs);

            for (var i = 0; i < orderedLogs.Length; i++)
            {
                var log = orderedLogs[i];

                // Only create segments for request entries — response entries
                // represent the instant the response arrives and contain no
                // internal processing spans.
                if (log.Type != RequestResponseType.Request)
                    continue;

                var segmentStart = log.Timestamp!.Value;
                var segmentEnd = i + 1 < orderedLogs.Length
                    ? orderedLogs[i + 1].Timestamp!.Value
                    : segmentStart.AddSeconds(5);

                var segmentSpans = testSpans
                    .Where(s => s.StartTimeUtc >= segmentStart.UtcDateTime &&
                                s.StartTimeUtc < segmentEnd.UtcDateTime)
                    .OrderBy(s => s.StartTimeUtc)
                    .ToArray();

                var segmentKey = $"iflow-{log.RequestResponseId}";

                segments[segmentKey] = new InternalFlowSegment(
                    log.RequestResponseId,
                    log.Type,
                    log.TestId,
                    segmentStart,
                    segmentEnd,
                    segmentSpans);
            }
        }

        return segments;
    }

    /// <summary>
    /// Builds a single whole-test segment per TestId containing all spans for that test.
    /// Keyed as "iflow-test-{TestId}". Tests with no matching spans are excluded.
    /// </summary>
    public static Dictionary<string, InternalFlowSegment> BuildWholeTestSegments(
        RequestResponseLog[] logs,
        Activity[] spans)
    {
        var segments = new Dictionary<string, InternalFlowSegment>();

        if (spans.Length == 0 || logs.Length == 0)
            return segments;

        var logsByTest = logs
            .Where(l => l.Timestamp.HasValue)
            .GroupBy(l => l.TestId);

        foreach (var testGroup in logsByTest)
        {
            var testLogs = testGroup.ToArray();
            var testSpans = FilterSpansByTestTraceIds(spans, testLogs);

            if (testSpans.Length == 0)
                continue;

            var orderedSpans = testSpans.OrderBy(s => s.StartTimeUtc).ToArray();
            var startTime = orderedSpans.Min(s => s.StartTimeUtc);
            var endTime = orderedSpans.Max(s => s.StartTimeUtc + s.Duration);

            var segmentKey = $"iflow-test-{testGroup.Key}";
            segments[segmentKey] = new InternalFlowSegment(
                Guid.Empty,
                RequestResponseType.Request,
                testGroup.Key,
                new DateTimeOffset(startTime, TimeSpan.Zero),
                new DateTimeOffset(endTime, TimeSpan.Zero),
                orderedSpans);
        }

        return segments;
    }

    private static Activity[] FilterSpansByTestTraceIds(Activity[] allSpans, RequestResponseLog[] testLogs)
    {
        var testActivityTraceIds = testLogs
            .Where(l => l.ActivityTraceId is not null)
            .Select(l => l.ActivityTraceId!)
            .ToHashSet();

        if (testActivityTraceIds.Count == 0)
            return allSpans;

        return allSpans
            .Where(s => testActivityTraceIds.Contains(s.TraceId.ToString()))
            .ToArray();
    }
}
