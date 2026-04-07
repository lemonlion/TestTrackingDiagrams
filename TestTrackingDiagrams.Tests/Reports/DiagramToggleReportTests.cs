using System.Diagnostics;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Reports;

public class DiagramToggleReportTests : IDisposable
{
    private readonly ActivitySource _source = new("Tests.DiagramToggle");
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = [];

    public DiagramToggleReportTests()
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

    private Activity CreateSpan(string name)
    {
        var span = _source.StartActivity(name)!;
        span.SetEndTime(span.StartTimeUtc + TimeSpan.FromMilliseconds(50));
        _activities.Add(span);
        return span;
    }

    private static Feature[] MakeFeatures(string testId = "t1", string name = "Test scenario") =>
    [
        new Feature
        {
            DisplayName = "Test Feature",
            Scenarios =
            [
                new Scenario
                {
                    Id = testId,
                    DisplayName = name,
                    IsHappyPath = true,
                    Result = ScenarioResult.Passed
                }
            ]
        }
    ];

    private static DefaultDiagramsFetcher.DiagramAsCode[] MakeDiagrams(string testId = "t1") =>
    [
        new(testId, "", "@startuml\nA->B: call\n@enduml")
    ];

    private Dictionary<string, InternalFlowSegment> MakeWholeTestSegments(string testId = "t1")
    {
        var span = CreateSpan("operation");
        return new Dictionary<string, InternalFlowSegment>
        {
            [$"iflow-test-{testId}"] = new(Guid.Empty, RequestResponseType.Request, testId,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMilliseconds(50), [span])
        };
    }

    private string GenerateReport(
        DefaultDiagramsFetcher.DiagramAsCode[] diagrams,
        Feature[] features,
        string fileName,
        Dictionary<string, InternalFlowSegment>? wholeTestSegments = null,
        WholeTestFlowVisualization visualization = WholeTestFlowVisualization.Both)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, fileName, "Test", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            internalFlowTracking: wholeTestSegments is not null,
            wholeTestSegments: wholeTestSegments,
            wholeTestVisualization: visualization);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Report_with_diagrams_only_shows_sequence_diagrams_summary()
    {
        var content = GenerateReport(MakeDiagrams(), MakeFeatures(), "ToggleSeqOnly.html");

        Assert.Contains("Sequence Diagrams</summary>", content);
        Assert.DoesNotContain("diagram-toggle", content);
    }

    [Fact]
    public void Report_with_diagrams_and_whole_test_flow_shows_toggle_buttons()
    {
        var content = GenerateReport(
            MakeDiagrams(), MakeFeatures(), "ToggleBoth.html",
            wholeTestSegments: MakeWholeTestSegments());

        Assert.Contains("diagram-toggle", content);
        Assert.Contains("data-dtype=\"seq\"", content);
        Assert.Contains("data-dtype=\"activity\"", content);
        Assert.Contains("data-dtype=\"flame\"", content);
        Assert.Contains(">Sequence Diagrams</button>", content);
        Assert.Contains(">Activity Diagrams</button>", content);
        Assert.Contains(">Flame Chart</button>", content);
    }

    [Fact]
    public void Report_with_diagrams_and_whole_test_flow_shows_diagrams_summary()
    {
        var content = GenerateReport(
            MakeDiagrams(), MakeFeatures(), "ToggleSummary.html",
            wholeTestSegments: MakeWholeTestSegments());

        Assert.Contains("Diagrams</summary>", content);
        Assert.DoesNotContain("Sequence Diagrams</summary>", content);
    }

    [Fact]
    public void Report_with_diagrams_and_whole_test_flow_has_sequence_diagrams_active_by_default()
    {
        var content = GenerateReport(
            MakeDiagrams(), MakeFeatures(), "ToggleDefault.html",
            wholeTestSegments: MakeWholeTestSegments());

        Assert.Contains("diagram-toggle-active\" data-dtype=\"seq\"", content);
    }

    [Fact]
    public void Report_with_diagrams_and_whole_test_flow_wraps_sequence_in_view_div()
    {
        var content = GenerateReport(
            MakeDiagrams(), MakeFeatures(), "ToggleSeqView.html",
            wholeTestSegments: MakeWholeTestSegments());

        Assert.Contains("diagram-view-seq", content);
        Assert.Contains("diagram-view-activity", content);
        Assert.Contains("diagram-view-flame", content);
    }

    [Fact]
    public void Report_with_diagrams_and_whole_test_flow_hides_activity_and_flame_by_default()
    {
        var content = GenerateReport(
            MakeDiagrams(), MakeFeatures(), "ToggleHidden.html",
            wholeTestSegments: MakeWholeTestSegments());

        Assert.Contains("diagram-view-activity\" style=\"display:none\"", content);
        Assert.Contains("diagram-view-flame\" style=\"display:none\"", content);
        Assert.DoesNotContain("diagram-view-seq\" style=\"display:none\"", content);
    }

    [Fact]
    public void Report_with_flame_chart_only_shows_two_toggle_buttons()
    {
        var content = GenerateReport(
            MakeDiagrams(), MakeFeatures(), "ToggleFlameOnly.html",
            wholeTestSegments: MakeWholeTestSegments(),
            visualization: WholeTestFlowVisualization.FlameChart);

        Assert.Contains("data-dtype=\"seq\"", content);
        Assert.Contains("data-dtype=\"flame\"", content);
        Assert.DoesNotContain("data-dtype=\"activity\"", content);
    }

    [Fact]
    public void Report_with_activity_only_shows_two_toggle_buttons()
    {
        var content = GenerateReport(
            MakeDiagrams(), MakeFeatures(), "ToggleActivityOnly.html",
            wholeTestSegments: MakeWholeTestSegments(),
            visualization: WholeTestFlowVisualization.ActivityDiagram);

        Assert.Contains("data-dtype=\"seq\"", content);
        Assert.Contains("data-dtype=\"activity\"", content);
        Assert.DoesNotContain("data-dtype=\"flame\"", content);
    }

    [Fact]
    public void Report_no_diagrams_no_whole_test_flow_has_no_diagram_section()
    {
        var content = GenerateReport(
            Array.Empty<DefaultDiagramsFetcher.DiagramAsCode>(),
            MakeFeatures(), "ToggleNone.html");

        Assert.DoesNotContain("<details class=\"example-diagrams\"", content);
        Assert.DoesNotContain("<div class=\"diagram-toggle\"", content);
    }

    [Fact]
    public void Report_no_whole_test_flow_data_for_visualization_Both_still_shows_sequence_only()
    {
        var content = GenerateReport(
            MakeDiagrams(), MakeFeatures(), "ToggleNoFlow.html",
            wholeTestSegments: new Dictionary<string, InternalFlowSegment>(),
            visualization: WholeTestFlowVisualization.Both);

        Assert.Contains("Sequence Diagrams</summary>", content);
        Assert.DoesNotContain("<div class=\"diagram-toggle\"", content);
    }

    [Fact]
    public void Report_whole_test_flow_only_no_sequence_diagrams_shows_diagrams_summary()
    {
        var content = GenerateReport(
            Array.Empty<DefaultDiagramsFetcher.DiagramAsCode>(),
            MakeFeatures(), "ToggleFlowOnly.html",
            wholeTestSegments: MakeWholeTestSegments());

        Assert.Contains("Diagrams</summary>", content);
        Assert.Contains("diagram-toggle", content);
        Assert.DoesNotContain("data-dtype=\"seq\"", content);
    }

    [Fact]
    public void Report_does_not_contain_whole_test_flow_as_separate_section()
    {
        var content = GenerateReport(
            MakeDiagrams(), MakeFeatures(), "ToggleNoWTFSection.html",
            wholeTestSegments: MakeWholeTestSegments());

        Assert.DoesNotContain("<details class=\"whole-test-flow\"", content);
        Assert.DoesNotContain("Whole Test Flow", content);
    }

    [Fact]
    public void Report_contains_diagram_toggle_css()
    {
        var content = GenerateReport(
            MakeDiagrams(), MakeFeatures(), "ToggleCss.html",
            wholeTestSegments: MakeWholeTestSegments());

        Assert.Contains("diagram-toggle-btn", content);
        Assert.Contains("diagram-toggle-active", content);
    }

    [Fact]
    public void Report_contains_diagram_toggle_javascript()
    {
        var content = GenerateReport(
            MakeDiagrams(), MakeFeatures(), "ToggleJs.html",
            wholeTestSegments: MakeWholeTestSegments());

        Assert.Contains("diagram-toggle-btn", content);
        Assert.Contains("data-dtype", content);
    }
}
