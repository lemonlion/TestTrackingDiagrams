using System.Net;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Reports;

[Collection("TrackingComponentRegistry")]
public class DiagnosticReportGeneratorTests : IDisposable
{
    public DiagnosticReportGeneratorTests()
    {
        TrackingComponentRegistry.Clear();
    }

    public void Dispose()
    {
        TrackingComponentRegistry.Clear();
    }

    [Fact]
    public void Unused_component_hint_mentions_ResolveDbContextOptions_not_PostConfigure()
    {
        TrackingComponentRegistry.Register(
            new StubComponent("SqlTrackingInterceptor (DB)", wasInvoked: false));

        var html = DiagnosticReportGenerator.BuildHtml(
            [MakeLog("t1", RequestResponseType.Request, Guid.NewGuid())],
            [],
            new ReportConfigurationOptions());

        Assert.Contains("ResolveDbContextOptions", html);
        Assert.DoesNotContain("Fix: use <code>PostConfigure</code> on the framework", html);
    }

    [Fact]
    public void Unused_component_hint_warns_PostConfigure_does_not_work_with_Duende()
    {
        TrackingComponentRegistry.Register(
            new StubComponent("SqlTrackingInterceptor (DB)", wasInvoked: false));

        var html = DiagnosticReportGenerator.BuildHtml(
            [MakeLog("t1", RequestResponseType.Request, Guid.NewGuid())],
            [],
            new ReportConfigurationOptions());

        Assert.Contains("PostConfigure", html);
        Assert.Contains("does not work with Duende IdentityServer", html);
    }

    [Fact]
    public void No_unused_component_hints_when_all_components_are_active()
    {
        TrackingComponentRegistry.Register(
            new StubComponent("SqlTrackingInterceptor (DB)", wasInvoked: true));

        var html = DiagnosticReportGenerator.BuildHtml(
            [MakeLog("t1", RequestResponseType.Request, Guid.NewGuid())],
            [],
            new ReportConfigurationOptions());

        Assert.DoesNotContain("Never Invoked", html);
        Assert.DoesNotContain("ResolveDbContextOptions", html);
    }

    [Fact]
    public void No_tracking_component_section_when_none_registered()
    {
        var html = DiagnosticReportGenerator.BuildHtml(
            [MakeLog("t1", RequestResponseType.Request, Guid.NewGuid())],
            [],
            new ReportConfigurationOptions());

        Assert.DoesNotContain("Tracking Components", html);
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
}
