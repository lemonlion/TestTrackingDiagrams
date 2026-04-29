using TestTrackingDiagrams.Extensions.Redis;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Redis;

public class RedisTrackingDatabaseTests : IDisposable
{
    // ─── Test infrastructure ────────────────────────────────────

    private readonly string _testId = Guid.NewGuid().ToString();

    private RequestResponseLog[] GetLogsFromThisTest()
    {
        return RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == _testId)
            .ToArray();
    }

    private RedisTrackingDatabaseOptions MakeOptions(
        RedisTrackingVerbosity verbosity = RedisTrackingVerbosity.Detailed,
        string serviceName = "Redis",
        string callerName = "TestCaller") => new()
    {
        ServiceName = serviceName,
        CallerName = callerName,
        Verbosity = verbosity,
        CurrentTestInfoFetcher = () => ("My Test", _testId),
    };

    public void Dispose()
    {
    }

    // ─── Basic logging ─────────────────────────────────────────

    [Fact]
    public void LogRedisRequest_And_LogRedisResponse_Logs_request_and_response()
    {
        var options = MakeOptions();
        var tracker = new RedisTracker(options);

        var (reqId, traceId) = tracker.LogRedisRequest("GET", "user:123", 0, null);
        tracker.LogRedisResponse("GET", "user:123", 0, true, reqId, traceId, "John");

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public void Logs_correct_service_and_caller_names()
    {
        var tracker = new RedisTracker(MakeOptions(callerName: "MyApi", serviceName: "Cache"));

        tracker.LogRedisRequest("GET", "key", 0, null);

        var log = GetLogsFromThisTest().First();
        Assert.Equal("Cache", log.ServiceName);
        Assert.Equal("MyApi", log.CallerName);
    }

    [Fact]
    public void Does_not_log_when_no_test_info_fetcher()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        var tracker = new RedisTracker(options);

        tracker.LogRedisRequest("GET", "key", 0, null);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    // ─── Cache hit/miss ────────────────────────────────────────

    [Fact]
    public void Get_Hit_LogsHitLabel()
    {
        var tracker = new RedisTracker(MakeOptions());

        var (reqId, traceId) = tracker.LogRedisRequest("GET", "user:123", 0, null);
        tracker.LogRedisResponse("GET", "user:123", 0, true, reqId, traceId, "John");

        var response = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal("Get (Hit)", response.Method.Value?.ToString());
    }

    [Fact]
    public void Get_Miss_LogsMissLabel()
    {
        var tracker = new RedisTracker(MakeOptions());

        var (reqId, traceId) = tracker.LogRedisRequest("GET", "user:999", 0, null);
        tracker.LogRedisResponse("GET", "user:999", 0, false, reqId, traceId, null);

        var response = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal("Get (Miss)", response.Method.Value?.ToString());
    }

    [Fact]
    public void HashGet_Hit_LogsHitLabel()
    {
        var tracker = new RedisTracker(MakeOptions());

        var (reqId, traceId) = tracker.LogRedisRequest("HGET", "user:123", 0, null);
        tracker.LogRedisResponse("HGET", "user:123", 0, true, reqId, traceId, "John");

        var response = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal("HashGet (Hit)", response.Method.Value?.ToString());
    }

    [Fact]
    public void HashGet_Miss_LogsMissLabel()
    {
        var tracker = new RedisTracker(MakeOptions());

        var (reqId, traceId) = tracker.LogRedisRequest("HGET", "user:999", 0, null);
        tracker.LogRedisResponse("HGET", "user:999", 0, false, reqId, traceId, null);

        var response = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal("HashGet (Miss)", response.Method.Value?.ToString());
    }

    [Fact]
    public void Set_Operation_LogsSetLabel()
    {
        var tracker = new RedisTracker(MakeOptions());

        var (reqId, traceId) = tracker.LogRedisRequest("SET", "user:123", 0, "John");
        tracker.LogRedisResponse("SET", "user:123", 0, false, reqId, traceId, null);

        var request = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Set", request.Method.Value?.ToString());
    }

    [Fact]
    public void Delete_Operation_LogsDeleteLabel()
    {
        var tracker = new RedisTracker(MakeOptions());

        var (reqId, traceId) = tracker.LogRedisRequest("DEL", "user:123", 0, null);
        tracker.LogRedisResponse("DEL", "user:123", 0, false, reqId, traceId, null);

        var request = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Delete", request.Method.Value?.ToString());
    }

    // ─── Request logs show the operation before result is known ──

    [Fact]
    public void Request_LogsOperationWithoutHitMiss()
    {
        var tracker = new RedisTracker(MakeOptions());

        tracker.LogRedisRequest("GET", "user:123", 0, null);

        var request = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Get", request.Method.Value?.ToString());
    }

    // ─── Detailed verbosity ────────────────────────────────────

    [Fact]
    public void Detailed_UsesClassifiedLabel()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Detailed));

        tracker.LogRedisRequest("SET", "user:123", 0, "John");

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Set", log.Method.Value?.ToString());
    }

    [Fact]
    public void Detailed_IncludesContentAsValue()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Detailed));

        tracker.LogRedisRequest("SET", "user:123", 0, "John");

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("John", log.Content);
    }

    [Fact]
    public void Detailed_BuildsCleanUriWithKey()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Detailed));

        tracker.LogRedisRequest("GET", "user:123", 0, null);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("redis://db0/user:123", log.Uri.ToString());
    }

    [Fact]
    public void Detailed_BuildsCleanUriWithDbNumber()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Detailed));

        tracker.LogRedisRequest("GET", "user:123", 3, null);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("redis://db3/user:123", log.Uri.ToString());
    }

    // ─── Summarised verbosity ──────────────────────────────────

    [Fact]
    public void Summarised_UsesClassifiedLabel()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Summarised));

        tracker.LogRedisRequest("SET", "user:123", 0, "John");

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Set", log.Method.Value?.ToString());
    }

    [Fact]
    public void Summarised_OmitsContent()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Summarised));

        tracker.LogRedisRequest("SET", "user:123", 0, "John");

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Null(log.Content);
    }

    [Fact]
    public void Summarised_UriShowsDbOnly()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Summarised));

        tracker.LogRedisRequest("GET", "user:123", 0, null);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("redis://db0/", log.Uri.ToString());
    }

    [Fact]
    public void Summarised_SkipsOtherOperations()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Summarised));

        tracker.LogRedisRequest("RANDOMCMD", "key", 0, null);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    [Fact]
    public void Summarised_DoesNotSkipGet()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Summarised));

        tracker.LogRedisRequest("GET", "key", 0, null);

        var logs = GetLogsFromThisTest();
        Assert.NotEmpty(logs);
    }

    // ─── Raw verbosity ─────────────────────────────────────────

    [Fact]
    public void Raw_UsesRawCommandName()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Raw));

        tracker.LogRedisRequest("GET", "user:123", 0, null);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("GET", log.Method.Value?.ToString());
    }

    [Fact]
    public void Raw_IncludesFullContent()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Raw));

        tracker.LogRedisRequest("SET", "user:123", 0, "John");

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("John", log.Content);
    }

    [Fact]
    public void Raw_DoesNotSkipOtherOperations()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Raw));

        tracker.LogRedisRequest("RANDOMCMD", "key", 0, null);

        var logs = GetLogsFromThisTest();
        Assert.NotEmpty(logs);
    }

    [Fact]
    public void Raw_UsesFullUriWithEndpoint()
    {
        var options = MakeOptions(RedisTrackingVerbosity.Raw);
        var tracker = new RedisTracker(options, "myredis.cache.windows.net:6380");

        tracker.LogRedisRequest("GET", "user:123", 0, null);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("redis://myredis.cache.windows.net:6380/0/user:123", log.Uri.ToString());
    }

    [Fact]
    public void Raw_DefaultEndpoint_UsesLocalhost()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Raw));

        tracker.LogRedisRequest("GET", "user:123", 0, null);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("redis://localhost/0/user:123", log.Uri.ToString());
    }

    [Fact]
    public void Raw_Response_Hit_LogsHitInStatusCode()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Raw));

        var (reqId, traceId) = tracker.LogRedisRequest("GET", "user:123", 0, null);
        tracker.LogRedisResponse("GET", "user:123", 0, true, reqId, traceId, "John");

        var response = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal("OK", response.StatusCode?.Value?.ToString());
    }

    [Fact]
    public void Response_Hit_IncludesValueAsContent_Detailed()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Detailed));

        var (reqId, traceId) = tracker.LogRedisRequest("GET", "user:123", 0, null);
        tracker.LogRedisResponse("GET", "user:123", 0, true, reqId, traceId, "John");

        var response = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal("John", response.Content);
    }

    [Fact]
    public void Response_Miss_NullContent()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Detailed));

        var (reqId, traceId) = tracker.LogRedisRequest("GET", "user:999", 0, null);
        tracker.LogRedisResponse("GET", "user:999", 0, false, reqId, traceId, null);

        var response = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Null(response.Content);
    }

    [Fact]
    public void Response_Summarised_OmitsContent()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Summarised));

        var (reqId, traceId) = tracker.LogRedisRequest("GET", "user:123", 0, null);
        tracker.LogRedisResponse("GET", "user:123", 0, true, reqId, traceId, "John");

        var response = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Null(response.Content);
    }

    // ─── Null key handling ─────────────────────────────────────

    [Fact]
    public void NullKey_Detailed_UriHasDbOnly()
    {
        var tracker = new RedisTracker(MakeOptions(RedisTrackingVerbosity.Detailed));

        tracker.LogRedisRequest("PUBLISH", null, 0, "message");

        var log = GetLogsFromThisTest().First();
        Assert.Equal("redis://db0/", log.Uri.ToString());
    }

    // ─── ITrackingComponent ────────────────────────────────────

    [Fact]
    public void Implements_ITrackingComponent()
    {
        var tracker = new RedisTracker(MakeOptions());
        Assert.IsAssignableFrom<ITrackingComponent>(tracker);
    }

    [Fact]
    public void WasInvoked_IsFalse_BeforeAnyCommands()
    {
        var tracker = new RedisTracker(MakeOptions());
        Assert.False(tracker.WasInvoked);
    }

    [Fact]
    public void WasInvoked_IsTrue_AfterCommand()
    {
        var tracker = new RedisTracker(MakeOptions());
        tracker.LogRedisRequest("GET", "key:1", 0, null);
        Assert.True(tracker.WasInvoked);
    }

    [Fact]
    public void InvocationCount_StartsAtZero()
    {
        var tracker = new RedisTracker(MakeOptions());
        Assert.Equal(0, tracker.InvocationCount);
    }

    [Fact]
    public void InvocationCount_IncreasesWithEachCommand()
    {
        var tracker = new RedisTracker(MakeOptions());
        tracker.LogRedisRequest("GET", "key:1", 0, null);
        tracker.LogRedisRequest("SET", "key:2", 0, "val");
        Assert.Equal(2, tracker.InvocationCount);
    }

    [Fact]
    public void ComponentName_MatchesServiceName()
    {
        var tracker = new RedisTracker(MakeOptions(serviceName: "MyRedis"));
        Assert.Equal("RedisTracker (MyRedis)", tracker.ComponentName);
    }

    [Fact]
    public void Constructor_AutoRegistersWithTrackingComponentRegistry()
    {
        TrackingComponentRegistry.Clear();
        var tracker = new RedisTracker(MakeOptions());

        var components = TrackingComponentRegistry.GetRegisteredComponents();
        Assert.Contains(components, c => ReferenceEquals(c, tracker));
    }
}
