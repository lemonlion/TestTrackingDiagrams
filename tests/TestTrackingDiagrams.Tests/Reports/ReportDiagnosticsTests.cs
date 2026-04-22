using System.Net;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Reports;

[Collection("TrackingComponentRegistry")]
public class ReportDiagnosticsTests : IDisposable
{
    public ReportDiagnosticsTests()
    {
        TrackingComponentRegistry.Clear();
    }

    public void Dispose()
    {
        TrackingComponentRegistry.Clear();
    }

    [Fact]
    public void Warns_when_unpaired_requests_exist()
    {
        var testId = Guid.NewGuid().ToString();
        var logs = new[]
        {
            MakeLog(testId, RequestResponseType.Request, Guid.NewGuid()),
            MakeLog(testId, RequestResponseType.Request, Guid.NewGuid())
        };

        var warnings = ReportDiagnostics.Analyse(logs, []);
        Assert.Contains(warnings, w => w.Contains("unpaired") && w.Contains("2"));
    }

    [Fact]
    public void No_warning_when_all_requests_are_paired()
    {
        var testId = Guid.NewGuid().ToString();
        var pairId = Guid.NewGuid();
        var logs = new[]
        {
            MakeLog(testId, RequestResponseType.Request, pairId),
            MakeLog(testId, RequestResponseType.Response, pairId)
        };

        var warnings = ReportDiagnostics.Analyse(logs, []);
        Assert.DoesNotContain(warnings, w => w.Contains("unpaired"));
    }

    [Fact]
    public void Warns_when_log_test_ids_dont_match_any_feature()
    {
        var orphanTestId = Guid.NewGuid().ToString();
        var pairId = Guid.NewGuid();
        var logs = new[]
        {
            MakeLog(orphanTestId, RequestResponseType.Request, pairId),
            MakeLog(orphanTestId, RequestResponseType.Response, pairId)
        };
        var features = new[] { MakeFeature("different-test-id") };

        var warnings = ReportDiagnostics.Analyse(logs, features);
        Assert.Contains(warnings, w => w.Contains("orphaned") && w.Contains("1"));
    }

    [Fact]
    public void No_orphaned_warning_when_test_ids_match()
    {
        var testId = Guid.NewGuid().ToString();
        var pairId = Guid.NewGuid();
        var logs = new[]
        {
            MakeLog(testId, RequestResponseType.Request, pairId),
            MakeLog(testId, RequestResponseType.Response, pairId)
        };
        var features = new[] { MakeFeature(testId) };

        var warnings = ReportDiagnostics.Analyse(logs, features);
        Assert.DoesNotContain(warnings, w => w.Contains("orphaned"));
    }

    [Fact]
    public void Returns_summary_with_total_counts()
    {
        var testId = Guid.NewGuid().ToString();
        var pairId = Guid.NewGuid();
        var logs = new[]
        {
            MakeLog(testId, RequestResponseType.Request, pairId),
            MakeLog(testId, RequestResponseType.Response, pairId)
        };
        var features = new[] { MakeFeature(testId) };

        var warnings = ReportDiagnostics.Analyse(logs, features);
        Assert.Contains(warnings, w => w.Contains("2 log entries") && w.Contains("1 test"));
    }

    [Fact]
    public void Returns_empty_for_no_logs_and_no_features()
    {
        var warnings = ReportDiagnostics.Analyse([], []);
        Assert.Empty(warnings);
    }

    // ─── Unused tracking component warnings ────────────────────

    [Fact]
    public void Warns_when_tracking_component_never_invoked()
    {
        TrackingComponentRegistry.Register(new StubComponent("SqlTrackingInterceptor (DB)", wasInvoked: false));
        var logs = new[] { MakeLog("t1", RequestResponseType.Request, Guid.NewGuid()) };

        var warnings = ReportDiagnostics.Analyse(logs, []);

        Assert.Contains(warnings, w => w.Contains("never invoked") && w.Contains("SqlTrackingInterceptor (DB)"));
    }

    [Fact]
    public void No_unused_component_warning_when_all_invoked()
    {
        TrackingComponentRegistry.Register(new StubComponent("Handler", wasInvoked: true));
        var logs = new[] { MakeLog("t1", RequestResponseType.Request, Guid.NewGuid()) };

        var warnings = ReportDiagnostics.Analyse(logs, []);

        Assert.DoesNotContain(warnings, w => w.Contains("never invoked"));
    }

    [Fact]
    public void No_unused_component_warning_when_no_components_registered()
    {
        var logs = new[] { MakeLog("t1", RequestResponseType.Request, Guid.NewGuid()) };

        var warnings = ReportDiagnostics.Analyse(logs, []);

        Assert.DoesNotContain(warnings, w => w.Contains("never invoked"));
    }

    [Fact]
    public void Unused_component_warning_lists_count()
    {
        TrackingComponentRegistry.Clear();
        TrackingComponentRegistry.Register(new StubComponent("A", wasInvoked: false));
        TrackingComponentRegistry.Register(new StubComponent("B", wasInvoked: false));
        TrackingComponentRegistry.Register(new StubComponent("C", wasInvoked: true));
        var logs = new[] { MakeLog("t1", RequestResponseType.Request, Guid.NewGuid()) };

        var warnings = ReportDiagnostics.Analyse(logs, []);

        Assert.Contains(warnings, w => w.Contains("2 tracking component(s)") && w.Contains("A") && w.Contains("B"));
    }

    [Fact]
    public void Unused_component_warning_does_not_throw()
    {
        TrackingComponentRegistry.Register(new StubComponent("DB", wasInvoked: false));
        var logs = new[] { MakeLog("t1", RequestResponseType.Request, Guid.NewGuid()) };

        // This should return warnings, NOT throw
        var warnings = ReportDiagnostics.Analyse(logs, []);

        Assert.NotEmpty(warnings);
    }

    // ─── Helpers ───────────────────────────────────────────────

    private class StubComponent(string name, bool wasInvoked) : ITrackingComponent
    {
        public string ComponentName => name;
        public bool WasInvoked => wasInvoked;
        public int InvocationCount => wasInvoked ? 1 : 0;
    }

    private static RequestResponseLog MakeLog(string testId, RequestResponseType type, Guid requestResponseId) =>
        new("Test", testId, HttpMethod.Get, null, new Uri("http://svc/api"),
            [], "Svc", "Caller", type, Guid.NewGuid(), requestResponseId, false)
        {
            Timestamp = DateTimeOffset.UtcNow
        };

    private static Feature MakeFeature(string testId) =>
        new()
        {
            DisplayName = "Feature",
            Scenarios =
            [
                new Scenario { Id = testId, DisplayName = "Scenario" }
            ]
        };
}
