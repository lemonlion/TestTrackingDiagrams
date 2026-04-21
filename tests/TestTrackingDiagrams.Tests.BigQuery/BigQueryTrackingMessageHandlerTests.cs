using System.Net;
using TestTrackingDiagrams.Extensions.BigQuery;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.BigQuery;

public class BigQueryTrackingMessageHandlerTests : IDisposable
{
    private const string BaseUrl = "https://bigquery.googleapis.com/bigquery/v2/projects/my-project";
    private readonly StubInnerHandler _innerHandler = new();
    private readonly string _testId = Guid.NewGuid().ToString();

    private RequestResponseLog[] GetLogsFromThisTest()
    {
        return RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == _testId)
            .ToArray();
    }

    private HttpMessageInvoker CreateInvoker(BigQueryTrackingMessageHandlerOptions options)
    {
        var handler = new BigQueryTrackingMessageHandler(options, _innerHandler);
        return new HttpMessageInvoker(handler);
    }

    private BigQueryTrackingMessageHandlerOptions MakeOptions(
        BigQueryTrackingVerbosity verbosity = BigQueryTrackingVerbosity.Detailed,
        string serviceName = "BigQuery",
        string callerName = "TestCaller") => new()
    {
        ServiceName = serviceName,
        CallingServiceName = callerName,
        Verbosity = verbosity,
        CurrentTestInfoFetcher = () => ("My Test", _testId),
    };

    private static HttpRequestMessage MakeQueryRequest()
    {
        return new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/queries")
        {
            Content = new StringContent("""{"query":"SELECT * FROM dataset.table","useLegacySql":false}""")
        };
    }

    private static HttpRequestMessage MakeInsertRequest()
    {
        return new HttpRequestMessage(HttpMethod.Post,
            $"{BaseUrl}/datasets/mydataset/tables/mytable/insertAll")
        {
            Content = new StringContent("""{"rows":[{"json":{"id":"1","name":"test"}}]}""")
        };
    }

    private static HttpRequestMessage MakeReadTableRequest()
    {
        return new HttpRequestMessage(HttpMethod.Get,
            $"{BaseUrl}/datasets/mydataset/tables/mytable");
    }

    private static HttpRequestMessage MakeUnrecognisedRequest()
    {
        return new HttpRequestMessage(HttpMethod.Get,
            "https://bigquery.googleapis.com/discovery/v1/apis");
    }

    public void Dispose()
    {
        _innerHandler.Dispose();
    }

    // ─── Basic logging ─────────────────────────────────────────

    [Fact]
    public async Task Logs_request_and_response_for_each_call()
    {
        using var invoker = CreateInvoker(MakeOptions());

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public async Task Logs_correct_service_and_caller_names()
    {
        using var invoker = CreateInvoker(MakeOptions(callerName: "MyApi", serviceName: "OrdersBigQuery"));

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal("OrdersBigQuery", logs[0].ServiceName);
        Assert.Equal("MyApi", logs[0].CallerName);
    }

    [Fact]
    public async Task Does_not_log_when_no_test_info_fetcher()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task Request_is_still_forwarded_when_no_test_info_fetcher()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        Assert.NotNull(_innerHandler.CapturedRequest);
    }

    [Fact]
    public async Task Request_and_response_share_same_traceId_and_requestResponseId()
    {
        using var invoker = CreateInvoker(MakeOptions());

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(logs[0].TraceId, logs[1].TraceId);
        Assert.Equal(logs[0].RequestResponseId, logs[1].RequestResponseId);
    }

    // ─── Detailed verbosity ────────────────────────────────────

    [Fact]
    public async Task Detailed_Query_UsesClassifiedLabel()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Query", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Detailed_Insert_UsesClassifiedLabel()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeInsertRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Insert", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Detailed_Read_UsesClassifiedLabel()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeReadTableRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Read", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Detailed_IncludesRequestContent()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("SELECT * FROM dataset.table", log.Content);
    }

    [Fact]
    public async Task Detailed_IncludesResponseContent()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeReadTableRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Contains("queryResponse", log.Content);
    }

    [Fact]
    public async Task Detailed_BuildsCleanUri()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeReadTableRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("mydataset", log.Uri.AbsolutePath);
        Assert.Contains("mytable", log.Uri.AbsolutePath);
    }

    // ─── Summarised verbosity ──────────────────────────────────

    [Fact]
    public async Task Summarised_UsesOperationNameOnly()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Query", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Summarised_OmitsRequestContent()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Null(log.Content);
    }

    [Fact]
    public async Task Summarised_OmitsResponseContent()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeReadTableRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Null(log.Content);
    }

    [Fact]
    public async Task Summarised_OmitsHeaders()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeReadTableRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Empty(log.Headers);
    }

    [Fact]
    public async Task Summarised_SkipsOtherOperations()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeUnrecognisedRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task Summarised_BuildsSimpleUri()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeInsertRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        // Summarised: just resource type and name
        Assert.Contains("mytable", log.Uri.AbsolutePath);
    }

    // ─── Raw verbosity ────────────────────────────────────────

    [Fact]
    public async Task Raw_UsesHttpMethodAsMethod()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal(HttpMethod.Post, log.Method.Value);
    }

    [Fact]
    public async Task Raw_IncludesFullContent()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeInsertRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("rows", log.Content);
    }

    [Fact]
    public async Task Raw_DoesNotSkipOtherOperations()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeUnrecognisedRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public async Task Raw_UsesOriginalUri()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeReadTableRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal($"{BaseUrl}/datasets/mydataset/tables/mytable", log.Uri.ToString());
    }

    // ─── Header filtering ─────────────────────────────────────

    [Fact]
    public async Task Detailed_ExcludesDefaultNoisyHeaders()
    {
        using var invoker = CreateInvoker(MakeOptions(BigQueryTrackingVerbosity.Detailed));
        var request = MakeReadTableRequest();
        request.Headers.Add("x-goog-api-client", "gl-dotnet/10.0.0");
        request.Headers.Add("x-custom-header", "keep-me");

        await invoker.SendAsync(request, CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.DoesNotContain(log.Headers, h => h.Key == "x-goog-api-client");
        Assert.Contains(log.Headers, h => h.Key == "x-custom-header");
    }

    // ─── StatusCode ───────────────────────────────────────────

    [Fact]
    public async Task Response_log_includes_status_code()
    {
        using var invoker = CreateInvoker(MakeOptions());

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.NotNull(log.StatusCode);
        Assert.Equal(HttpStatusCode.OK, log.StatusCode!.Value);
    }

    // ─── ITrackingComponent ────────────────────────────────────

    [Fact]
    public void Implements_ITrackingComponent()
    {
        var handler = new BigQueryTrackingMessageHandler(MakeOptions());
        Assert.IsAssignableFrom<ITrackingComponent>(handler);
    }

    [Fact]
    public void WasInvoked_IsFalse_BeforeAnyRequests()
    {
        var handler = new BigQueryTrackingMessageHandler(MakeOptions());
        Assert.False(handler.WasInvoked);
    }

    [Fact]
    public async Task WasInvoked_IsTrue_AfterRequest()
    {
        var inner = new StubInnerHandler();
        var handler = new BigQueryTrackingMessageHandler(MakeOptions(), inner);
        using var invoker = new HttpMessageInvoker(handler);
        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        Assert.True(handler.WasInvoked);
    }

    [Fact]
    public void InvocationCount_StartsAtZero()
    {
        var handler = new BigQueryTrackingMessageHandler(MakeOptions());
        Assert.Equal(0, handler.InvocationCount);
    }

    [Fact]
    public void ComponentName_MatchesServiceName()
    {
        var handler = new BigQueryTrackingMessageHandler(MakeOptions(serviceName: "MyBQ"));
        Assert.Equal("BigQueryTrackingMessageHandler (MyBQ)", handler.ComponentName);
    }

    [Fact]
    public void Constructor_AutoRegistersWithTrackingComponentRegistry()
    {
        TrackingComponentRegistry.Clear();
        var handler = new BigQueryTrackingMessageHandler(MakeOptions());

        var components = TrackingComponentRegistry.GetRegisteredComponents();
        Assert.Contains(components, c => ReferenceEquals(c, handler));
    }
}
