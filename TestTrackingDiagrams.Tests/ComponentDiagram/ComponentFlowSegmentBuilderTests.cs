using System.Diagnostics;
using System.Net;
using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.ComponentDiagram;

public class ComponentFlowSegmentBuilderTests : IDisposable
{
    private readonly ActivitySource _source = new("TestTrackingDiagrams.Tests.CompFlow");
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = [];

    public ComponentFlowSegmentBuilderTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        foreach (var a in _activities) a.Dispose();
        _listener.Dispose();
        _source.Dispose();
    }

    private Activity CreateSpan(string name, DateTime startUtc, TimeSpan duration, string? traceId = null)
    {
        Activity.Current = null;
        var ctx = traceId != null
            ? new ActivityContext(ActivityTraceId.CreateFromString(traceId), default, ActivityTraceFlags.Recorded)
            : new ActivityContext(ActivityTraceId.CreateRandom(), default, ActivityTraceFlags.Recorded);

        var span = _source.StartActivity(name, ActivityKind.Internal, ctx)!;
        span.SetStartTime(startUtc);
        span.SetEndTime(startUtc + duration);
        _activities.Add(span);
        return span;
    }

    private static RequestResponseLog MakeRequest(
        string testId = "test-1",
        string caller = "Caller",
        string service = "OrderService",
        DateTimeOffset? timestamp = null,
        Guid? requestResponseId = null,
        string? activityTraceId = null)
    {
        return new RequestResponseLog(
            TestName: "Test",
            TestId: testId,
            Method: HttpMethod.Get,
            Content: null,
            Uri: new Uri("http://sut/api/orders"),
            Headers: [],
            ServiceName: service,
            CallerName: caller,
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: requestResponseId ?? Guid.NewGuid(),
            TrackingIgnore: false)
        {
            Timestamp = timestamp,
            ActivityTraceId = activityTraceId
        };
    }

    // ── BuildRelationshipSegments ──

    [Fact]
    public void BuildRelationshipSegments_empty_returns_empty()
    {
        var result = ComponentFlowSegmentBuilder.BuildRelationshipSegments([], [], []);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildRelationshipSegments_groups_spans_by_relationship()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var span = CreateSpan("op", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5));

        var logs = new[]
        {
            MakeRequest(caller: "API", service: "DB", timestamp: baseTime, activityTraceId: null)
        };

        var relationships = ComponentDiagramGenerator.ExtractRelationships(logs);

        var perBoundarySegments = InternalFlowSegmentBuilder.BuildSegments(logs, [span]);

        var result = ComponentFlowSegmentBuilder.BuildRelationshipSegments(
            relationships, logs, perBoundarySegments);

        Assert.Single(result);
        var key = result.Keys.First();
        Assert.Contains("API", key);
        Assert.Contains("DB", key);
    }

    [Fact]
    public void BuildRelationshipSegments_aggregates_across_tests()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var traceId1 = "0af7651916cd43dd8448eb211c80319c";
        var traceId2 = "1bf8762a27de54ee9559fc322d91420d";

        var span1 = CreateSpan("op1", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5), traceId: traceId1);
        var span2 = CreateSpan("op2", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5), traceId: traceId2);

        var logs = new[]
        {
            MakeRequest(testId: "t1", caller: "API", service: "DB", timestamp: baseTime, activityTraceId: traceId1),
            MakeRequest(testId: "t2", caller: "API", service: "DB", timestamp: baseTime, activityTraceId: traceId2)
        };

        var relationships = ComponentDiagramGenerator.ExtractRelationships(logs);
        var perBoundarySegments = InternalFlowSegmentBuilder.BuildSegments(logs, [span1, span2]);

        var result = ComponentFlowSegmentBuilder.BuildRelationshipSegments(
            relationships, logs, perBoundarySegments);

        Assert.Single(result);
        var segment = result.Values.First();
        Assert.Equal(2, segment.AggregatedSegment.Spans.Length);
    }

    [Fact]
    public void BuildRelationshipSegments_includes_test_summaries()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var span1 = CreateSpan("op1", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(50));

        var logs = new[]
        {
            MakeRequest(testId: "t1", caller: "API", service: "DB", timestamp: baseTime, activityTraceId: null)
        };

        var relationships = ComponentDiagramGenerator.ExtractRelationships(logs);
        var perBoundarySegments = InternalFlowSegmentBuilder.BuildSegments(logs, [span1]);

        var result = ComponentFlowSegmentBuilder.BuildRelationshipSegments(
            relationships, logs, perBoundarySegments);

        var entry = result.Values.First();
        Assert.Single(entry.TestSummaries);
        Assert.Equal("t1", entry.TestSummaries[0].TestId);
    }

    // ── BuildSystemSegment (obsolete but tested for backward compat) ──

#pragma warning disable CS0618
    [Fact]
    public void BuildSystemSegment_empty_returns_empty()
    {
        var result = ComponentFlowSegmentBuilder.BuildSystemSegment([]);
        Assert.Empty(result.Spans);
    }

    [Fact]
    public void BuildSystemSegment_merges_all_spans()
    {
        var span1 = CreateSpan("op1", DateTime.UtcNow, TimeSpan.FromMilliseconds(10));
        var span2 = CreateSpan("op2", DateTime.UtcNow.AddMilliseconds(20), TimeSpan.FromMilliseconds(10));

        var wholeTestSegments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-t1"] = new(Guid.Empty, RequestResponseType.Request, "t1", null, null, [span1]),
            ["iflow-test-t2"] = new(Guid.Empty, RequestResponseType.Request, "t2", null, null, [span2])
        };

        var result = ComponentFlowSegmentBuilder.BuildSystemSegment(wholeTestSegments);

        Assert.Equal(2, result.Spans.Length);
    }
#pragma warning restore CS0618
}
