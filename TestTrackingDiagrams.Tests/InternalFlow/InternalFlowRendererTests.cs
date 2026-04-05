using System.Diagnostics;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.InternalFlow;

public class InternalFlowRendererTests : IDisposable
{
    private readonly ActivitySource _source = new("TestTrackingDiagrams.Tests.InternalFlow");
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = [];

    public InternalFlowRendererTests()
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

    private Activity CreateSpan(string name, Activity? parent = null, TimeSpan? duration = null)
    {
        Activity? span;
        if (parent != null)
        {
            var ctx = new ActivityContext(parent.TraceId, parent.SpanId, ActivityTraceFlags.Recorded);
            span = _source.StartActivity(name, ActivityKind.Internal, ctx)!;
        }
        else
        {
            span = _source.StartActivity(name)!;
        }

        if (duration.HasValue)
        {
            // Set duration by stopping after the specified time
            span.SetEndTime(span.StartTimeUtc + duration.Value);
        }
        else
        {
            span.SetEndTime(span.StartTimeUtc + TimeSpan.FromMilliseconds(10));
        }

        _activities.Add(span);
        return span;
    }

    private static InternalFlowSegment MakeSegment(params Activity[] spans) =>
        new(Guid.NewGuid(), RequestResponseType.Request, "test-1",
            spans.Length > 0 ? spans.Min(s => s.StartTimeUtc) : null,
            spans.Length > 0 ? spans.Max(s => s.StartTimeUtc + s.Duration) : null,
            spans);

    // ── BuildSpanTree ──

    [Fact]
    public void BuildSpanTree_with_duplicate_SpanIds_does_not_throw()
    {
        var span1 = CreateSpan("op1");
        // Use the same span twice to simulate duplicate SpanId
        var spans = new[] { span1, span1 };

        var roots = InternalFlowRenderer.BuildSpanTree(spans);

        Assert.Single(roots);
    }

    [Fact]
    public void BuildSpanTree_builds_parent_child_hierarchy()
    {
        var parent = CreateSpan("parent");
        var child = CreateSpan("child", parent);

        var roots = InternalFlowRenderer.BuildSpanTree([parent, child]);

        Assert.Single(roots);
        Assert.Equal("parent", roots[0].Span.OperationName);
        Assert.Single(roots[0].Children);
        Assert.Equal("child", roots[0].Children[0].Span.OperationName);
    }

    [Fact]
    public void BuildSpanTree_sorts_roots_by_start_time()
    {
        // Create two independent root spans with explicit start times — no sleeping needed
        var earlyTime = DateTime.UtcNow;
        var lateTime = earlyTime.AddSeconds(1);

        Activity.Current = null;
        var early = _source.StartActivity("early", ActivityKind.Internal,
            new ActivityContext(ActivityTraceId.CreateRandom(), default, ActivityTraceFlags.Recorded))!;
        early.SetStartTime(earlyTime);
        early.SetEndTime(earlyTime.AddMilliseconds(10));
        _activities.Add(early);

        Activity.Current = null;
        var late = _source.StartActivity("late", ActivityKind.Internal,
            new ActivityContext(ActivityTraceId.CreateRandom(), default, ActivityTraceFlags.Recorded))!;
        late.SetStartTime(lateTime);
        late.SetEndTime(lateTime.AddMilliseconds(10));
        _activities.Add(late);

        var roots = InternalFlowRenderer.BuildSpanTree([late, early]);

        Assert.Equal(2, roots.Count);
        Assert.Equal("early", roots[0].Span.OperationName);
        Assert.Equal("late", roots[1].Span.OperationName);
    }

    [Fact]
    public void BuildSpanTree_orphan_children_become_roots()
    {
        // Create a child whose parent is not in the span array
        var parent = CreateSpan("parent");
        var child = CreateSpan("child", parent);

        // Only include the child
        var roots = InternalFlowRenderer.BuildSpanTree([child]);

        Assert.Single(roots);
        Assert.Equal("child", roots[0].Span.OperationName);
    }

    [Fact]
    public void BuildSpanTree_empty_array_returns_empty()
    {
        var roots = InternalFlowRenderer.BuildSpanTree([]);
        Assert.Empty(roots);
    }

    // ── RenderActivityDiagram ──

    [Fact]
    public void RenderActivityDiagram_empty_spans_returns_empty()
    {
        var result = InternalFlowRenderer.RenderActivityDiagram(MakeSegment());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RenderActivityDiagram_produces_plantuml_with_swimlanes()
    {
        var parent = CreateSpan("HTTP GET /api");
        var child = CreateSpan("SELECT * FROM users", parent);

        var result = InternalFlowRenderer.RenderActivityDiagram(MakeSegment(parent, child));

        Assert.Contains("@startuml", result);
        Assert.Contains("@enduml", result);
        Assert.Contains("HTTP GET /api", result);
        Assert.Contains("SELECT * FROM users", result);
    }

    [Fact]
    public void RenderActivityDiagram_with_duplicate_SpanIds_does_not_throw()
    {
        var span = CreateSpan("op");
        var result = InternalFlowRenderer.RenderActivityDiagram(MakeSegment(span, span));

        Assert.Contains("@startuml", result);
        Assert.Contains("op", result);
    }

    // ── RenderCallTree ──

    [Fact]
    public void RenderCallTree_empty_spans_returns_empty()
    {
        var result = InternalFlowRenderer.RenderCallTree(MakeSegment());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RenderCallTree_produces_nested_html_list()
    {
        var parent = CreateSpan("HTTP GET /api");
        var child = CreateSpan("DB Query", parent);

        var result = InternalFlowRenderer.RenderCallTree(MakeSegment(parent, child));

        Assert.Contains("<ul", result);
        Assert.Contains("HTTP GET /api", result);
        Assert.Contains("DB Query", result);
    }

    [Fact]
    public void RenderCallTree_with_duplicate_SpanIds_does_not_throw()
    {
        var span = CreateSpan("op");
        var result = InternalFlowRenderer.RenderCallTree(MakeSegment(span, span));

        Assert.Contains("<ul", result);
    }

    // ── RenderFlameChart ──

    [Fact]
    public void RenderFlameChart_empty_spans_returns_empty()
    {
        var result = InternalFlowRenderer.RenderFlameChart(MakeSegment());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RenderFlameChart_produces_flame_bars()
    {
        var parent = CreateSpan("HTTP GET /api", duration: TimeSpan.FromMilliseconds(100));
        var child = CreateSpan("DB Query", parent, duration: TimeSpan.FromMilliseconds(50));

        var result = InternalFlowRenderer.RenderFlameChart(MakeSegment(parent, child));

        Assert.Contains("iflow-flame", result);
        Assert.Contains("iflow-flame-bar", result);
        Assert.Contains("HTTP GET /api", result);
        Assert.Contains("DB Query", result);
    }

    [Fact]
    public void RenderFlameChart_with_duplicate_SpanIds_does_not_throw()
    {
        var span = CreateSpan("op", duration: TimeSpan.FromMilliseconds(10));
        var result = InternalFlowRenderer.RenderFlameChart(MakeSegment(span, span));

        Assert.Contains("iflow-flame", result);
    }
}
