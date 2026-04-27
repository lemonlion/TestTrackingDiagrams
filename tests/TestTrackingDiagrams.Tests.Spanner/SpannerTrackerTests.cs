using TestTrackingDiagrams.Extensions.Spanner;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Spanner;

public class SpannerTrackerTests
{
    private readonly string _testId = Guid.NewGuid().ToString();

    private RequestResponseLog[] GetLogsFromThisTest()
    {
        return RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == _testId)
            .ToArray();
    }

    private SpannerTrackingOptions MakeOptions(
        SpannerTrackingVerbosity verbosity = SpannerTrackingVerbosity.Detailed,
        string serviceName = "Spanner",
        string callerName = "TestCaller") => new()
    {
        ServiceName = serviceName,
        CallingServiceName = callerName,
        Verbosity = verbosity,
        CurrentTestInfoFetcher = () => ("My Spanner Test", _testId),
    };

    // ─── LogRequest ─────────────────────────────────────────

    [Fact]
    public void LogRequest_Logs_request_entry()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        tracker.LogRequest(op, "SELECT * FROM Users");

        var logs = GetLogsFromThisTest();
        Assert.Single(logs);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
    }

    [Fact]
    public void LogRequest_Returns_ids_for_pairing()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        var (reqId, traceId) = tracker.LogRequest(op, "SELECT * FROM Users");

        Assert.NotEqual(Guid.Empty, reqId);
        Assert.NotEqual(Guid.Empty, traceId);
    }

    [Fact]
    public void LogRequest_NoTestInfo_NoLog()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        var tracker = new SpannerTracker(options);
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        var (reqId, traceId) = tracker.LogRequest(op, "SELECT * FROM Users");

        Assert.Empty(GetLogsFromThisTest());
        Assert.Equal(Guid.Empty, reqId);
    }

    [Fact]
    public void LogRequest_Uses_event_metatype()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        tracker.LogRequest(op, "SELECT * FROM Users");

        var log = GetLogsFromThisTest().First();
        Assert.Equal(RequestResponseMetaType.Event, log.MetaType);
    }

    [Fact]
    public void LogRequest_Uses_Database_dependency_category()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        tracker.LogRequest(op, "SELECT * FROM Users");

        var log = GetLogsFromThisTest().First();
        Assert.Equal("Database", log.DependencyCategory);
    }

    // ─── LogResponse ────────────────────────────────────────

    [Fact]
    public void LogResponse_Logs_response_entry()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");
        var (reqId, traceId) = tracker.LogRequest(op, "SELECT * FROM Users");

        tracker.LogResponse(op, reqId, traceId, "2 rows returned");

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public void LogResponse_MatchesTraceId()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");
        var (reqId, traceId) = tracker.LogRequest(op, "SELECT * FROM Users");

        tracker.LogResponse(op, reqId, traceId, null);

        var logs = GetLogsFromThisTest();
        Assert.Equal(logs[0].TraceId, logs[1].TraceId);
    }

    // ─── Verbosity ──────────────────────────────────────────

    [Fact]
    public void LogRequest_Detailed_IncludesContent()
    {
        var tracker = new SpannerTracker(MakeOptions(SpannerTrackingVerbosity.Detailed));
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        tracker.LogRequest(op, "SELECT * FROM Users");

        var log = GetLogsFromThisTest().First();
        Assert.Equal("SELECT * FROM Users", log.Content);
    }

    [Fact]
    public void LogRequest_Summarised_OmitsContent()
    {
        var tracker = new SpannerTracker(MakeOptions(SpannerTrackingVerbosity.Summarised));
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        tracker.LogRequest(op, "SELECT * FROM Users");

        var log = GetLogsFromThisTest().First();
        Assert.Null(log.Content);
    }

    // ─── URI ────────────────────────────────────────────────

    [Fact]
    public void LogRequest_URI_uses_spanner_scheme()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        tracker.LogRequest(op, "SELECT * FROM Users");

        var log = GetLogsFromThisTest().First();
        Assert.StartsWith("spanner://", log.Uri.ToString());
    }

    [Fact]
    public void LogRequest_URI_Detailed_includes_table()
    {
        var tracker = new SpannerTracker(MakeOptions(SpannerTrackingVerbosity.Detailed));
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        tracker.LogRequest(op, "SELECT * FROM Users");

        var log = GetLogsFromThisTest().First();
        Assert.Contains("Users", log.Uri.ToString());
    }

    [Fact]
    public void LogRequest_URI_Raw_includes_database()
    {
        var tracker = new SpannerTracker(MakeOptions(SpannerTrackingVerbosity.Raw));
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users", DatabaseId: "mydb");

        tracker.LogRequest(op, "SELECT * FROM Users");

        var log = GetLogsFromThisTest().First();
        Assert.Contains("mydb", log.Uri.ToString());
    }

    // ─── Service/caller names ──────────────────────────────

    [Fact]
    public void LogRequest_Uses_configured_service_and_caller()
    {
        var tracker = new SpannerTracker(MakeOptions(serviceName: "MySpanner", callerName: "MyApi"));
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        tracker.LogRequest(op, "SELECT * FROM Users");

        var log = GetLogsFromThisTest().First();
        Assert.Equal("MySpanner", log.ServiceName);
        Assert.Equal("MyApi", log.CallerName);
    }

    // ─── ITrackingComponent ────────────────────────────────

    [Fact]
    public void Implements_ITrackingComponent()
    {
        var tracker = new SpannerTracker(MakeOptions());
        Assert.IsAssignableFrom<ITrackingComponent>(tracker);
    }

    [Fact]
    public void WasInvoked_IsFalse_BeforeAnyCalls()
    {
        var tracker = new SpannerTracker(MakeOptions());
        Assert.False(tracker.WasInvoked);
    }

    [Fact]
    public void WasInvoked_IsTrue_AfterLogRequest()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        tracker.LogRequest(op, "SELECT * FROM Users");

        Assert.True(tracker.WasInvoked);
    }

    [Fact]
    public void InvocationCount_Increments()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");

        tracker.LogRequest(op, "sql1");
        tracker.LogRequest(op, "sql2");

        Assert.Equal(2, tracker.InvocationCount);
    }

    [Fact]
    public void ComponentName_IncludesServiceName()
    {
        var tracker = new SpannerTracker(MakeOptions(serviceName: "MySpanner"));
        Assert.Contains("MySpanner", tracker.ComponentName);
    }

    // ─── Excluded operations ────────────────────────────────

    [Fact]
    public void LogRequest_ExcludedOperation_NoLog()
    {
        var options = MakeOptions();
        options.ExcludedOperations = [SpannerOperation.CreateSession];
        var tracker = new SpannerTracker(options);
        var op = new SpannerOperationInfo(SpannerOperation.CreateSession);

        var (reqId, traceId) = tracker.LogRequest(op, null);

        Assert.Empty(GetLogsFromThisTest());
        Assert.Equal(Guid.Empty, reqId);
    }
}
