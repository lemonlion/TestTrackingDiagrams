using System.Diagnostics;
using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.InternalFlow;

public class InternalFlowHtmlGeneratorTests : IDisposable
{
    private readonly ActivitySource _source = new("TestTrackingDiagrams.Tests.HtmlGenerator");
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = [];

    public InternalFlowHtmlGeneratorTests()
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

    private Activity CreateSpan(string name, TimeSpan? duration = null)
    {
        var span = _source.StartActivity(name)!;
        span.SetEndTime(span.StartTimeUtc + (duration ?? TimeSpan.FromMilliseconds(10)));
        _activities.Add(span);
        return span;
    }

    private static InternalFlowSegment MakeSegment(string testId, params Activity[] spans) =>
        new(Guid.Empty, RequestResponseType.Request, testId,
            spans.Length > 0 ? spans.Min(s => s.StartTimeUtc) : null,
            spans.Length > 0 ? spans.Max(s => s.StartTimeUtc + s.Duration) : null,
            spans);

    // ── GenerateWholeTestFlowHtml ──

    [Fact]
    public void GenerateWholeTestFlowHtml_returns_empty_for_empty_segments()
    {
        var segments = new Dictionary<string, InternalFlowSegment>();
        var result = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, "test-1", [], WholeTestFlowVisualization.Both);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GenerateWholeTestFlowHtml_returns_empty_for_None()
    {
        var span = CreateSpan("op");
        var segments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-test-1"] = MakeSegment("test-1", span)
        };

        var result = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, "test-1", [], WholeTestFlowVisualization.None);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GenerateWholeTestFlowHtml_produces_collapsed_details()
    {
        var span = CreateSpan("op", TimeSpan.FromMilliseconds(100));
        var segments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-test-1"] = MakeSegment("test-1", span)
        };

        var result = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, "test-1", [], WholeTestFlowVisualization.Both);

        Assert.Contains("<details", result);
        Assert.Contains("Whole Test Flow", result);
        Assert.DoesNotContain("open", result.Split('\n').First(l => l.Contains("<details")));
    }

    [Fact]
    public void GenerateWholeTestFlowHtml_Both_includes_toggle_buttons()
    {
        var span = CreateSpan("op", TimeSpan.FromMilliseconds(100));
        var segments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-test-1"] = MakeSegment("test-1", span)
        };

        var result = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, "test-1", [], WholeTestFlowVisualization.Both);

        Assert.Contains("iflow-toggle", result);
        Assert.Contains("Activity", result);
        Assert.Contains("Flame Chart", result);
    }

    [Fact]
    public void GenerateWholeTestFlowHtml_FlameChart_only_has_flamechart()
    {
        var span = CreateSpan("op", TimeSpan.FromMilliseconds(100));
        var segments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-test-1"] = MakeSegment("test-1", span)
        };

        var result = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, "test-1", [], WholeTestFlowVisualization.FlameChart);

        Assert.Contains("iflow-flame", result);
        Assert.DoesNotContain("iflow-toggle", result);
    }

    [Fact]
    public void GenerateWholeTestFlowHtml_ActivityDiagram_only_has_activity()
    {
        var span = CreateSpan("op", TimeSpan.FromMilliseconds(100));
        var segments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-test-1"] = MakeSegment("test-1", span)
        };

        var result = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, "test-1", [], WholeTestFlowVisualization.ActivityDiagram);

        Assert.Contains("plantuml-browser", result);
        Assert.DoesNotContain("iflow-toggle", result);
    }

    [Fact]
    public void GenerateWholeTestFlowHtml_no_matching_test_returns_empty()
    {
        var span = CreateSpan("op");
        var segments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-test-1"] = MakeSegment("test-1", span)
        };

        var result = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, "test-999", [], WholeTestFlowVisualization.Both);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GenerateWholeTestFlowHtml_FlameChart_uses_compressed_attribute()
    {
        var span = CreateSpan("op", TimeSpan.FromMilliseconds(100));
        var segments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-test-1"] = MakeSegment("test-1", span)
        };

        var result = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, "test-1", [], WholeTestFlowVisualization.FlameChart);

        Assert.Contains("data-flame-z=", result);
        Assert.DoesNotContain("data-flame=\"", result);
    }

    [Fact]
    public void GenerateWholeTestFlowHtml_ActivityDiagram_uses_compressed_attribute()
    {
        var span = CreateSpan("op", TimeSpan.FromMilliseconds(100));
        var segments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-test-1"] = MakeSegment("test-1", span)
        };

        var result = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, "test-1", [], WholeTestFlowVisualization.ActivityDiagram);

        Assert.Contains("data-plantuml-z=", result);
        Assert.DoesNotContain("data-plantuml=\"", result);
    }

    [Fact]
    public void CompressToBase64_round_trips_correctly()
    {
        var original = "{\"s\":[\"test\"],\"f\":[[0,\"operation\",1.23,45.67,0,100]]}";
        var compressed = InternalFlowHtmlGenerator.CompressToBase64(original);

        // Decompress to verify
        var bytes = Convert.FromBase64String(compressed);
        using var input = new System.IO.MemoryStream(bytes);
        using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var reader = new System.IO.StreamReader(gzip);
        var decompressed = reader.ReadToEnd();

        Assert.Equal(original, decompressed);
    }

    // ── GenerateRelationshipPopupContent ──

    [Fact]
    public void GenerateRelationshipPopupContent_ActivityDiagram_ContainsPlantUmlDiv()
    {
        var span = CreateSpan("op1", TimeSpan.FromMilliseconds(50));
        var segment = MakeSegment("aggregated", span);
        var flowData = new RelationshipFlowData(segment, [
            new RelationshipTestSummary("t1", "Test One", 1, 50)
        ]);

        var result = InternalFlowHtmlGenerator.GenerateRelationshipPopupContent(
            flowData, InternalFlowDiagramStyle.ActivityDiagram);

        Assert.Contains("plantuml-browser", result);
        Assert.Contains("iflow-rel-summary-table", result);
    }

    [Fact]
    public void GenerateRelationshipPopupContent_CallTree_ContainsCallTreeHtml()
    {
        var span = CreateSpan("op1", TimeSpan.FromMilliseconds(50));
        var segment = MakeSegment("aggregated", span);
        var flowData = new RelationshipFlowData(segment, [
            new RelationshipTestSummary("t1", "Test One", 1, 50)
        ]);

        var result = InternalFlowHtmlGenerator.GenerateRelationshipPopupContent(
            flowData, InternalFlowDiagramStyle.CallTree);

        Assert.Contains("iflow-call-tree", result);
        Assert.DoesNotContain("plantuml-browser", result);
    }

    [Fact]
    public void GenerateRelationshipPopupContent_IncludesSummaryTableWithTestInfo()
    {
        var span = CreateSpan("op1", TimeSpan.FromMilliseconds(50));
        var segment = MakeSegment("aggregated", span);
        var flowData = new RelationshipFlowData(segment, [
            new RelationshipTestSummary("t1", "My Test Name", 3, 120),
            new RelationshipTestSummary("t2", "Another Test", 1, 30)
        ]);

        var result = InternalFlowHtmlGenerator.GenerateRelationshipPopupContent(
            flowData, InternalFlowDiagramStyle.ActivityDiagram);

        Assert.Contains("iflow-rel-summary-table", result);
        Assert.Contains("My Test Name", result);
        Assert.Contains("Another Test", result);
        Assert.Contains("3", result); // SpanCount
        Assert.Contains("120ms", result); // duration
    }

    [Fact]
    public void GenerateRelationshipPopupContent_ManyTests_LimitsToTopTwenty()
    {
        var span = CreateSpan("op1", TimeSpan.FromMilliseconds(50));
        var segment = MakeSegment("aggregated", span);
        var summaries = Enumerable.Range(1, 25)
            .Select(i => new RelationshipTestSummary($"t{i}", $"Test {i}", 1, i * 10.0))
            .OrderByDescending(s => s.DurationMs)
            .ToArray();
        var flowData = new RelationshipFlowData(segment, summaries);

        var result = InternalFlowHtmlGenerator.GenerateRelationshipPopupContent(
            flowData, InternalFlowDiagramStyle.ActivityDiagram);

        // Should show top 20 and a "more" indicator
        Assert.Contains("and 5 more", result);
        // Should contain the highest duration test (Test 25)
        Assert.Contains("Test 25", result);
    }
}
