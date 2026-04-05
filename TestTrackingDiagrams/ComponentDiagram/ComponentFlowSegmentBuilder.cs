using System.Diagnostics;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.ComponentDiagram;

public record RelationshipFlowData(
    InternalFlowSegment AggregatedSegment,
    RelationshipTestSummary[] TestSummaries);

public record RelationshipTestSummary(
    string TestId,
    string TestName,
    int SpanCount,
    double DurationMs);

public static class ComponentFlowSegmentBuilder
{
    /// <summary>
    /// Builds aggregated flow data per relationship, grouping spans from all tests
    /// that exercised each (Caller, Service) pair.
    /// </summary>
    public static Dictionary<string, RelationshipFlowData> BuildRelationshipSegments(
        ComponentRelationship[] relationships,
        RequestResponseLog[] logs,
        Dictionary<string, InternalFlowSegment> perBoundarySegments)
    {
        var result = new Dictionary<string, RelationshipFlowData>();

        if (relationships.Length == 0 || perBoundarySegments.Count == 0)
            return result;

        foreach (var rel in relationships)
        {
            var relKey = $"iflow-rel-{SanitizeKey(rel.Caller)}-{SanitizeKey(rel.Service)}";

            var matchingLogIds = logs
                .Where(l => l.CallerName == rel.Caller && l.ServiceName == rel.Service
                            && l.Type == RequestResponseType.Request)
                .Select(l => l.RequestResponseId)
                .ToHashSet();

            var allSpans = new List<Activity>();
            var testSpans = new Dictionary<string, List<Activity>>();

            foreach (var (segKey, segment) in perBoundarySegments)
            {
                if (!matchingLogIds.Contains(segment.RequestResponseId))
                    continue;

                allSpans.AddRange(segment.Spans);

                if (!testSpans.TryGetValue(segment.TestId, out var list))
                {
                    list = [];
                    testSpans[segment.TestId] = list;
                }
                list.AddRange(segment.Spans);
            }

            if (allSpans.Count == 0)
                continue;

            var ordered = allSpans.OrderBy(s => s.StartTimeUtc).ToArray();
            var aggregated = new InternalFlowSegment(
                Guid.Empty, RequestResponseType.Request, "aggregated",
                new DateTimeOffset(ordered.Min(s => s.StartTimeUtc), TimeSpan.Zero),
                new DateTimeOffset(ordered.Max(s => s.StartTimeUtc + s.Duration), TimeSpan.Zero),
                ordered);

            var testName = logs
                .Where(l => l.CallerName == rel.Caller && l.ServiceName == rel.Service)
                .ToDictionary(l => l.TestId, l => l.TestName);

            var summaries = testSpans
                .Select(kv =>
                {
                    var spans = kv.Value;
                    var totalDuration = spans.Sum(s => s.Duration.TotalMilliseconds);
                    return new RelationshipTestSummary(
                        kv.Key,
                        testName.GetValueOrDefault(kv.Key, kv.Key),
                        spans.Count,
                        totalDuration);
                })
                .OrderByDescending(s => s.DurationMs)
                .ToArray();

            result[relKey] = new RelationshipFlowData(aggregated, summaries);
        }

        return result;
    }

    /// <summary>
    /// Builds a single system-level segment containing all spans across all tests.
    /// </summary>
    public static InternalFlowSegment BuildSystemSegment(
        Dictionary<string, InternalFlowSegment> wholeTestSegments)
    {
        var allSpans = wholeTestSegments.Values
            .SelectMany(s => s.Spans)
            .OrderBy(s => s.StartTimeUtc)
            .ToArray();

        if (allSpans.Length == 0)
            return new InternalFlowSegment(Guid.Empty, RequestResponseType.Request, "system", null, null, []);

        return new InternalFlowSegment(
            Guid.Empty, RequestResponseType.Request, "system",
            new DateTimeOffset(allSpans.Min(s => s.StartTimeUtc), TimeSpan.Zero),
            new DateTimeOffset(allSpans.Max(s => s.StartTimeUtc + s.Duration), TimeSpan.Zero),
            allSpans);
    }

    private static string SanitizeKey(string name) =>
        name.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
}
