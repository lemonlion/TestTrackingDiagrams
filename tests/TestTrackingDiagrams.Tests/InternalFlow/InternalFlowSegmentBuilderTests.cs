using System.Diagnostics;
using System.Net;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.InternalFlow;

public class InternalFlowSegmentBuilderTests : IDisposable
{
    private readonly ActivitySource _source = new("TestTrackingDiagrams.Tests.SegmentBuilder");
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = [];

    public InternalFlowSegmentBuilderTests()
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
            ServiceName: "OrderService",
            CallerName: "Caller",
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: requestResponseId ?? Guid.NewGuid(),
            TrackingIgnore: false)
        {
            Timestamp = timestamp,
            ActivityTraceId = activityTraceId
        };
    }

    private static RequestResponseLog MakeResponse(
        string testId = "test-1",
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
            ServiceName: "OrderService",
            CallerName: "Caller",
            Type: RequestResponseType.Response,
            TraceId: Guid.NewGuid(),
            RequestResponseId: requestResponseId ?? Guid.NewGuid(),
            TrackingIgnore: false,
            StatusCode: HttpStatusCode.OK)
        {
            Timestamp = timestamp,
            ActivityTraceId = activityTraceId
        };
    }

    // ── Empty inputs ──

    [Fact]
    public void BuildSegments_no_spans_returns_empty()
    {
        var logs = new[] { MakeRequest(timestamp: DateTimeOffset.UtcNow) };
        var result = InternalFlowSegmentBuilder.BuildSegments(logs, []);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildSegments_no_logs_returns_empty()
    {
        var span = CreateSpan("op", DateTime.UtcNow, TimeSpan.FromMilliseconds(10));
        var result = InternalFlowSegmentBuilder.BuildSegments([], [span]);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildSegments_logs_without_timestamps_returns_empty()
    {
        var span = CreateSpan("op", DateTime.UtcNow, TimeSpan.FromMilliseconds(10));
        var logs = new[] { MakeRequest(timestamp: null) };
        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [span]);
        Assert.Empty(result);
    }

    // ── Basic segment creation ──

    [Fact]
    public void BuildSegments_creates_segment_for_request_log()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId = Guid.NewGuid();

        var span = CreateSpan("HTTP GET", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(50));
        var logs = new[] { MakeRequest(timestamp: baseTime, requestResponseId: reqId) };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [span]);

        Assert.Single(result);
        Assert.True(result.ContainsKey($"iflow-{reqId}"));
        Assert.Single(result[$"iflow-{reqId}"].Spans);
    }

    [Fact]
    public void BuildSegments_skips_response_entries()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId = Guid.NewGuid();
        var resId = Guid.NewGuid();

        var span = CreateSpan("op", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5));
        var logs = new[]
        {
            MakeRequest(timestamp: baseTime, requestResponseId: reqId),
            MakeResponse(timestamp: baseTime.AddMilliseconds(100), requestResponseId: resId)
        };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [span]);

        Assert.Single(result);
        Assert.True(result.ContainsKey($"iflow-{reqId}"));
        Assert.False(result.ContainsKey($"iflow-{resId}"));
    }

    // ── Time window logic ──

    [Fact]
    public void BuildSegments_assigns_spans_to_correct_time_windows()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId1 = Guid.NewGuid();
        var reqId2 = Guid.NewGuid();

        var span1 = CreateSpan("early-op", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5));
        var span2 = CreateSpan("late-op", baseTime.UtcDateTime.AddMilliseconds(510), TimeSpan.FromMilliseconds(5));

        var logs = new[]
        {
            MakeRequest(timestamp: baseTime, requestResponseId: reqId1),
            MakeRequest(timestamp: baseTime.AddMilliseconds(500), requestResponseId: reqId2)
        };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [span1, span2]);

        Assert.Equal(2, result.Count);
        Assert.Single(result[$"iflow-{reqId1}"].Spans);
        Assert.Equal("early-op", result[$"iflow-{reqId1}"].Spans[0].OperationName);
        Assert.Single(result[$"iflow-{reqId2}"].Spans);
        Assert.Equal("late-op", result[$"iflow-{reqId2}"].Spans[0].OperationName);
    }

    [Fact]
    public void BuildSegments_last_segment_uses_5_second_fallback_window()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId = Guid.NewGuid();

        // Span within 5-second window
        var spanInWindow = CreateSpan("in-window", baseTime.UtcDateTime.AddSeconds(2), TimeSpan.FromMilliseconds(5));
        // Span outside 5-second window
        var spanOutWindow = CreateSpan("out-window", baseTime.UtcDateTime.AddSeconds(6), TimeSpan.FromMilliseconds(5));

        var logs = new[] { MakeRequest(timestamp: baseTime, requestResponseId: reqId) };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [spanInWindow, spanOutWindow]);

        Assert.Single(result);
        Assert.Single(result[$"iflow-{reqId}"].Spans);
        Assert.Equal("in-window", result[$"iflow-{reqId}"].Spans[0].OperationName);
    }

    [Fact]
    public void BuildSegments_span_exactly_at_segment_start_is_included()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId = Guid.NewGuid();

        var span = CreateSpan("at-boundary", baseTime.UtcDateTime, TimeSpan.FromMilliseconds(5));
        var logs = new[] { MakeRequest(timestamp: baseTime, requestResponseId: reqId) };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [span]);

        Assert.Single(result[$"iflow-{reqId}"].Spans);
    }

    [Fact]
    public void BuildSegments_span_exactly_at_segment_end_is_excluded()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId1 = Guid.NewGuid();
        var reqId2 = Guid.NewGuid();
        var boundaryTime = baseTime.AddMilliseconds(500);

        var span = CreateSpan("at-boundary", boundaryTime.UtcDateTime, TimeSpan.FromMilliseconds(5));
        var logs = new[]
        {
            MakeRequest(timestamp: baseTime, requestResponseId: reqId1),
            MakeRequest(timestamp: boundaryTime, requestResponseId: reqId2)
        };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [span]);

        // Span at boundary should be in second segment (>= start), not first (< end)
        Assert.Empty(result[$"iflow-{reqId1}"].Spans);
        Assert.Single(result[$"iflow-{reqId2}"].Spans);
    }

    // ── Trace ID filtering ──

    [Fact]
    public void BuildSegments_filters_spans_by_trace_id()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId = Guid.NewGuid();
        var goodTraceId = "0af7651916cd43dd8448eb211c80319c";
        var badTraceId = "aaaabbbbccccddddeeee111122223333";

        var matchingSpan = CreateSpan("matching", baseTime.UtcDateTime.AddMilliseconds(10),
            TimeSpan.FromMilliseconds(5), traceId: goodTraceId);
        var nonMatchingSpan = CreateSpan("non-matching", baseTime.UtcDateTime.AddMilliseconds(20),
            TimeSpan.FromMilliseconds(5), traceId: badTraceId);

        var logs = new[]
        {
            MakeRequest(timestamp: baseTime, requestResponseId: reqId, activityTraceId: goodTraceId)
        };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [matchingSpan, nonMatchingSpan]);

        Assert.Single(result[$"iflow-{reqId}"].Spans);
        Assert.Equal("matching", result[$"iflow-{reqId}"].Spans[0].OperationName);
    }

    [Fact]
    public void BuildSegments_null_trace_ids_returns_all_spans()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId = Guid.NewGuid();

        var span1 = CreateSpan("op1", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5));
        var span2 = CreateSpan("op2", baseTime.UtcDateTime.AddMilliseconds(20), TimeSpan.FromMilliseconds(5));

        // No ActivityTraceId set — should fall back to returning all spans
        var logs = new[] { MakeRequest(timestamp: baseTime, requestResponseId: reqId, activityTraceId: null) };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [span1, span2]);

        Assert.Equal(2, result[$"iflow-{reqId}"].Spans.Length);
    }

    // ── Multi-test grouping ──

    [Fact]
    public void BuildSegments_groups_by_test_id()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId1 = Guid.NewGuid();
        var reqId2 = Guid.NewGuid();

        var span1 = CreateSpan("test1-op", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5));
        var span2 = CreateSpan("test2-op", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5));

        var logs = new[]
        {
            MakeRequest(testId: "test-1", timestamp: baseTime, requestResponseId: reqId1, activityTraceId: null),
            MakeRequest(testId: "test-2", timestamp: baseTime, requestResponseId: reqId2, activityTraceId: null)
        };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [span1, span2]);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey($"iflow-{reqId1}"));
        Assert.True(result.ContainsKey($"iflow-{reqId2}"));
    }

    // ── Segment ordering ──

    [Fact]
    public void BuildSegments_orders_spans_by_start_time_within_segment()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId = Guid.NewGuid();

        var lateSpan = CreateSpan("late", baseTime.UtcDateTime.AddMilliseconds(50), TimeSpan.FromMilliseconds(5));
        var earlySpan = CreateSpan("early", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5));

        var logs = new[] { MakeRequest(timestamp: baseTime, requestResponseId: reqId) };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [lateSpan, earlySpan]);

        Assert.Equal(2, result[$"iflow-{reqId}"].Spans.Length);
        Assert.Equal("early", result[$"iflow-{reqId}"].Spans[0].OperationName);
        Assert.Equal("late", result[$"iflow-{reqId}"].Spans[1].OperationName);
    }

    // ── Edge case: span before first log ──

    [Fact]
    public void BuildSegments_span_before_first_log_is_excluded()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId = Guid.NewGuid();

        var earlySpan = CreateSpan("too-early", baseTime.UtcDateTime.AddMilliseconds(-100), TimeSpan.FromMilliseconds(5));
        var logs = new[] { MakeRequest(timestamp: baseTime, requestResponseId: reqId) };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [earlySpan]);

        Assert.Empty(result[$"iflow-{reqId}"].Spans);
    }

    // ── BuildWholeTestSegments ──

    [Fact]
    public void BuildWholeTestSegments_no_spans_returns_empty()
    {
        var logs = new[] { MakeRequest(timestamp: DateTimeOffset.UtcNow) };
        var result = InternalFlowSegmentBuilder.BuildWholeTestSegments(logs, []);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildWholeTestSegments_no_logs_returns_empty()
    {
        var span = CreateSpan("op", DateTime.UtcNow, TimeSpan.FromMilliseconds(10));
        var result = InternalFlowSegmentBuilder.BuildWholeTestSegments([], [span]);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildWholeTestSegments_creates_one_segment_per_test()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var span1 = CreateSpan("op1", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(50));
        var span2 = CreateSpan("op2", baseTime.UtcDateTime.AddMilliseconds(100), TimeSpan.FromMilliseconds(50));

        var logs = new[]
        {
            MakeRequest(testId: "test-1", timestamp: baseTime, activityTraceId: null),
            MakeRequest(testId: "test-1", timestamp: baseTime.AddMilliseconds(200), activityTraceId: null)
        };

        var result = InternalFlowSegmentBuilder.BuildWholeTestSegments(logs, [span1, span2]);

        Assert.Single(result);
        Assert.True(result.ContainsKey("iflow-test-test-1"));
        Assert.Equal(2, result["iflow-test-test-1"].Spans.Length);
    }

    [Fact]
    public void BuildWholeTestSegments_key_uses_iflow_test_prefix()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var span = CreateSpan("op", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5));
        var logs = new[] { MakeRequest(testId: "my-test", timestamp: baseTime, activityTraceId: null) };

        var result = InternalFlowSegmentBuilder.BuildWholeTestSegments(logs, [span]);

        Assert.True(result.ContainsKey("iflow-test-my-test"));
    }

    [Fact]
    public void BuildWholeTestSegments_includes_all_spans_for_test()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var traceId = "0af7651916cd43dd8448eb211c80319c";

        var span1 = CreateSpan("early", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5), traceId: traceId);
        var span2 = CreateSpan("middle", baseTime.UtcDateTime.AddMilliseconds(500), TimeSpan.FromMilliseconds(5), traceId: traceId);
        var span3 = CreateSpan("late", baseTime.UtcDateTime.AddSeconds(3), TimeSpan.FromMilliseconds(5), traceId: traceId);

        var logs = new[]
        {
            MakeRequest(testId: "test-1", timestamp: baseTime, activityTraceId: traceId),
            MakeRequest(testId: "test-1", timestamp: baseTime.AddMilliseconds(400), activityTraceId: traceId)
        };

        var result = InternalFlowSegmentBuilder.BuildWholeTestSegments(logs, [span1, span2, span3]);

        Assert.Equal(3, result["iflow-test-test-1"].Spans.Length);
    }

    [Fact]
    public void BuildWholeTestSegments_filters_by_trace_id()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var goodTraceId = "0af7651916cd43dd8448eb211c80319c";
        var badTraceId = "aaaabbbbccccddddeeee111122223333";

        var matching = CreateSpan("match", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5), traceId: goodTraceId);
        var nonMatching = CreateSpan("no-match", baseTime.UtcDateTime.AddMilliseconds(20), TimeSpan.FromMilliseconds(5), traceId: badTraceId);

        var logs = new[] { MakeRequest(testId: "test-1", timestamp: baseTime, activityTraceId: goodTraceId) };

        var result = InternalFlowSegmentBuilder.BuildWholeTestSegments(logs, [matching, nonMatching]);

        Assert.Single(result["iflow-test-test-1"].Spans);
        Assert.Equal("match", result["iflow-test-test-1"].Spans[0].OperationName);
    }

    [Fact]
    public void BuildWholeTestSegments_multiple_tests_get_separate_segments()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var span1 = CreateSpan("test1-op", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5));
        var span2 = CreateSpan("test2-op", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5));

        var logs = new[]
        {
            MakeRequest(testId: "test-1", timestamp: baseTime, activityTraceId: null),
            MakeRequest(testId: "test-2", timestamp: baseTime, activityTraceId: null)
        };

        var result = InternalFlowSegmentBuilder.BuildWholeTestSegments(logs, [span1, span2]);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("iflow-test-test-1"));
        Assert.True(result.ContainsKey("iflow-test-test-2"));
    }

    [Fact]
    public void BuildWholeTestSegments_orders_spans_by_start_time()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var late = CreateSpan("late", baseTime.UtcDateTime.AddMilliseconds(100), TimeSpan.FromMilliseconds(5));
        var early = CreateSpan("early", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5));

        var logs = new[] { MakeRequest(testId: "test-1", timestamp: baseTime, activityTraceId: null) };

        var result = InternalFlowSegmentBuilder.BuildWholeTestSegments(logs, [late, early]);

        Assert.Equal("early", result["iflow-test-test-1"].Spans[0].OperationName);
        Assert.Equal("late", result["iflow-test-test-1"].Spans[1].OperationName);
    }

    [Fact]
    public void BuildWholeTestSegments_empty_test_with_no_matching_spans_excluded()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var traceId1 = "0af7651916cd43dd8448eb211c80319c";
        var traceId2 = "aaaabbbbccccddddeeee111122223333";

        var span = CreateSpan("op", baseTime.UtcDateTime.AddMilliseconds(10), TimeSpan.FromMilliseconds(5), traceId: traceId1);

        var logs = new[]
        {
            MakeRequest(testId: "test-1", timestamp: baseTime, activityTraceId: traceId1),
            MakeRequest(testId: "test-2", timestamp: baseTime, activityTraceId: traceId2)
        };

        var result = InternalFlowSegmentBuilder.BuildWholeTestSegments(logs, [span]);

        Assert.Single(result);
        Assert.True(result.ContainsKey("iflow-test-test-1"));
    }

    // ── Response-based time windows ──

    [Fact]
    public void BuildSegments_uses_matching_response_timestamp_as_segment_end()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId = Guid.NewGuid();

        // Span at T+200ms — only within window if response timestamp (T+500ms) is used as end,
        // NOT if next-log timestamp were used
        var span = CreateSpan("during-processing", baseTime.UtcDateTime.AddMilliseconds(200),
            TimeSpan.FromMilliseconds(5));

        var logs = new[]
        {
            MakeRequest(timestamp: baseTime, requestResponseId: reqId),
            MakeResponse(timestamp: baseTime.AddMilliseconds(500), requestResponseId: reqId)
        };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [span]);

        Assert.Single(result);
        Assert.Single(result[$"iflow-{reqId}"].Spans);
        Assert.Equal("during-processing", result[$"iflow-{reqId}"].Spans[0].OperationName);
    }

    [Fact]
    public void BuildSegments_outer_request_captures_spans_across_nested_sub_calls()
    {
        // Simulates: Caller→SUT at T0, SUT→ServiceA at T1, ServiceA→SUT at T2, SUT→Caller at T3
        // The Caller→SUT segment should capture spans throughout the entire [T0, T3) window
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var outerReqId = Guid.NewGuid();
        var innerReqId = Guid.NewGuid();

        var earlySpan = CreateSpan("validation", baseTime.UtcDateTime.AddMilliseconds(5),
            TimeSpan.FromMilliseconds(10));
        var midSpan = CreateSpan("transform-response", baseTime.UtcDateTime.AddMilliseconds(250),
            TimeSpan.FromMilliseconds(10));
        var lateSpan = CreateSpan("finalize", baseTime.UtcDateTime.AddMilliseconds(450),
            TimeSpan.FromMilliseconds(10));

        var logs = new[]
        {
            MakeRequest(timestamp: baseTime, requestResponseId: outerReqId),
            MakeRequest(timestamp: baseTime.AddMilliseconds(100), requestResponseId: innerReqId),
            MakeResponse(timestamp: baseTime.AddMilliseconds(200), requestResponseId: innerReqId),
            MakeResponse(timestamp: baseTime.AddMilliseconds(500), requestResponseId: outerReqId)
        };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [earlySpan, midSpan, lateSpan]);

        // Outer segment should capture ALL three spans (full processing window)
        var outerSegment = result[$"iflow-{outerReqId}"];
        Assert.Equal(3, outerSegment.Spans.Length);
        Assert.Equal("validation", outerSegment.Spans[0].OperationName);
        Assert.Equal("transform-response", outerSegment.Spans[1].OperationName);
        Assert.Equal("finalize", outerSegment.Spans[2].OperationName);

        // Inner segment only captures spans during the sub-call window [T+100, T+200)
        var innerSegment = result[$"iflow-{innerReqId}"];
        Assert.Empty(innerSegment.Spans); // No spans start during the sub-call wait
    }

    [Fact]
    public void BuildSegments_no_response_falls_back_to_next_log_timestamp()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reqId1 = Guid.NewGuid();
        var reqId2 = Guid.NewGuid();

        var earlySpan = CreateSpan("early", baseTime.UtcDateTime.AddMilliseconds(10),
            TimeSpan.FromMilliseconds(5));
        var lateSpan = CreateSpan("late", baseTime.UtcDateTime.AddMilliseconds(510),
            TimeSpan.FromMilliseconds(5));

        // No response logs — falls back to next request's timestamp
        var logs = new[]
        {
            MakeRequest(timestamp: baseTime, requestResponseId: reqId1),
            MakeRequest(timestamp: baseTime.AddMilliseconds(500), requestResponseId: reqId2)
        };

        var result = InternalFlowSegmentBuilder.BuildSegments(logs, [earlySpan, lateSpan]);

        Assert.Single(result[$"iflow-{reqId1}"].Spans);
        Assert.Equal("early", result[$"iflow-{reqId1}"].Spans[0].OperationName);
        Assert.Single(result[$"iflow-{reqId2}"].Spans);
        Assert.Equal("late", result[$"iflow-{reqId2}"].Spans[0].OperationName);
    }
}
