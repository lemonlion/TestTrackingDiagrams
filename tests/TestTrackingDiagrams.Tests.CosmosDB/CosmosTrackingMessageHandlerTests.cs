using System.Net;
using TestTrackingDiagrams.Extensions.CosmosDB;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.CosmosDB;

public class CosmosTrackingMessageHandlerTests : IDisposable
{
    // ─── Test infrastructure ────────────────────────────────────

    private class StubInnerHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK) { Content = new StringContent("""{"id":"doc1","_rid":"abc=="}""") };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(ResponseToReturn);
        }
    }

    private readonly StubInnerHandler _innerHandler = new();
    private readonly string _testId = Guid.NewGuid().ToString();

    private RequestResponseLog[] GetLogsFromThisTest()
    {
        return RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == _testId)
            .ToArray();
    }

    private HttpMessageInvoker CreateInvoker(CosmosTrackingMessageHandlerOptions options)
    {
        var handler = new CosmosTrackingMessageHandler(options, _innerHandler);
        return new HttpMessageInvoker(handler);
    }

    private CosmosTrackingMessageHandlerOptions MakeOptions(
        CosmosTrackingVerbosity verbosity = CosmosTrackingVerbosity.Detailed,
        string serviceName = "CosmosDB",
        string callerName = "TestCaller") => new()
    {
        ServiceName = serviceName,
        CallingServiceName = callerName,
        Verbosity = verbosity,
        CurrentTestInfoFetcher = () => ("My Test", _testId),
    };

    private static HttpRequestMessage MakeCreateRequest()
    {
        return new HttpRequestMessage(HttpMethod.Post,
            "https://account.documents.azure.com/dbs/mydb/colls/orders/docs")
        {
            Content = new StringContent("""{"id":"order-1","total":42.50}""")
        };
    }

    private static HttpRequestMessage MakeReadRequest()
    {
        return new HttpRequestMessage(HttpMethod.Get,
            "https://account.documents.azure.com/dbs/mydb/colls/orders/docs/order-1");
    }

    private static HttpRequestMessage MakeQueryRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://account.documents.azure.com/dbs/mydb/colls/orders/docs")
        {
            Content = new StringContent("""{"query":"SELECT * FROM c WHERE c.status = 'active'","parameters":[]}""")
        };
        request.Headers.Add("x-ms-documentdb-isquery", "True");
        return request;
    }

    private static HttpRequestMessage MakeDeleteRequest()
    {
        return new HttpRequestMessage(HttpMethod.Delete,
            "https://account.documents.azure.com/dbs/mydb/colls/orders/docs/order-1");
    }

    private static HttpRequestMessage MakeMetadataRequest()
    {
        return new HttpRequestMessage(HttpMethod.Get,
            "https://account.documents.azure.com/dbs/mydb/colls/orders/pkranges");
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

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public async Task Logs_correct_service_and_caller_names()
    {
        using var invoker = CreateInvoker(MakeOptions(callerName: "MyApi", serviceName: "OrdersCosmosDB"));

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal("OrdersCosmosDB", logs[0].ServiceName);
        Assert.Equal("MyApi", logs[0].CallerName);
    }

    [Fact]
    public async Task Does_not_log_when_no_test_info_fetcher()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task Request_is_still_forwarded_when_no_test_info_fetcher()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        Assert.NotNull(_innerHandler.CapturedRequest);
    }

    // ─── Detailed verbosity ────────────────────────────────────

    [Fact]
    public async Task Detailed_Create_UsesClassifiedLabel()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Create", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Detailed_Read_UsesClassifiedLabel()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeReadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Read", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Detailed_Query_ShowsQueryTextAsContent()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeQueryRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Query", log.Method.Value?.ToString());
        Assert.Equal("SELECT * FROM c WHERE c.status = 'active'", log.Content);
    }

    [Fact]
    public async Task Detailed_IncludesResponseContent()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeReadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Contains("doc1", log.Content);
    }

    // ─── Summarised verbosity ──────────────────────────────────

    [Fact]
    public async Task Summarised_UsesOperationNameOnly()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Create", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Summarised_OmitsRequestContent()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Null(log.Content);
    }

    [Fact]
    public async Task Summarised_OmitsResponseContent()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeReadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Null(log.Content);
    }

    [Fact]
    public async Task Summarised_OmitsHeaders()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeReadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Empty(log.Headers);
    }

    [Fact]
    public async Task Summarised_SkipsMetadataOperations()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeMetadataRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    // ─── Raw verbosity ────────────────────────────────────────

    [Fact]
    public async Task Raw_UsesHttpMethodAsMethod()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal(HttpMethod.Post, log.Method.Value);
    }

    [Fact]
    public async Task Raw_IncludesFullContent()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("order-1", log.Content);
    }

    [Fact]
    public async Task Raw_DoesNotSkipMetadataOperations()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeMetadataRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    // ─── Header filtering ─────────────────────────────────────

    [Fact]
    public async Task Detailed_ExcludesDefaultNoisyHeaders()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Detailed));
        var request = MakeReadRequest();
        request.Headers.Add("x-ms-date", "Tue, 29 Mar 2016 02:03:06 GMT");
        request.Headers.Add("x-ms-custom-header", "keep-me");

        await invoker.SendAsync(request, CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.DoesNotContain(log.Headers, h => h.Key == "x-ms-date");
        Assert.Contains(log.Headers, h => h.Key == "x-ms-custom-header");
    }

    // ─── Response status code ─────────────────────────────────

    [Fact]
    public async Task Response_CapturesStatusCode()
    {
        _innerHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{}")
        };
        using var invoker = CreateInvoker(MakeOptions());

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal(HttpStatusCode.Created, log.StatusCode!.Value);
    }

    // ─── URI cleaning ─────────────────────────────────────────

    [Fact]
    public async Task Detailed_Create_UsesCleanUri_WithoutDb()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("/colls/orders", log.Uri.AbsolutePath);
    }

    [Fact]
    public async Task Detailed_Read_IncludesDocPathInUri()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeReadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("/colls/orders/docs/order-1", log.Uri.AbsolutePath);
    }

    [Fact]
    public async Task Summarised_UsesCollectionNameOnlyAsPath()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeReadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("/orders", log.Uri.AbsolutePath);
    }

    [Fact]
    public async Task Raw_UsesOriginalUri()
    {
        using var invoker = CreateInvoker(MakeOptions(CosmosTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("/docs", log.Uri.AbsolutePath);
    }

        // ─── ITrackingComponent ────────────────────────────────────

        [Fact]
        public void Implements_ITrackingComponent()
        {
            var handler = new CosmosTrackingMessageHandler(MakeOptions());
            Assert.IsAssignableFrom<ITrackingComponent>(handler);
        }

        [Fact]
        public void WasInvoked_IsFalse_BeforeAnyRequests()
        {
            var handler = new CosmosTrackingMessageHandler(MakeOptions());
            Assert.False(handler.WasInvoked);
        }

        [Fact]
        public async Task WasInvoked_IsTrue_AfterRequest()
        {
            var inner = new StubInnerHandler();
            var handler = new CosmosTrackingMessageHandler(MakeOptions(), inner);
            using var invoker = new HttpMessageInvoker(handler);
            await invoker.SendAsync(MakeCreateRequest(), CancellationToken.None);

            Assert.True(handler.WasInvoked);
        }

        [Fact]
        public void InvocationCount_StartsAtZero()
        {
            var handler = new CosmosTrackingMessageHandler(MakeOptions());
            Assert.Equal(0, handler.InvocationCount);
        }

        [Fact]
        public void ComponentName_MatchesServiceName()
        {
            var handler = new CosmosTrackingMessageHandler(MakeOptions(serviceName: "MyCosmosDB"));
            Assert.Equal("CosmosTrackingMessageHandler (MyCosmosDB)", handler.ComponentName);
        }

        [Fact]
        public void Constructor_AutoRegistersWithTrackingComponentRegistry()
        {
            TrackingComponentRegistry.Clear();
            var handler = new CosmosTrackingMessageHandler(MakeOptions());

            var components = TrackingComponentRegistry.GetRegisteredComponents();
            Assert.Contains(components, c => ReferenceEquals(c, handler));
        }
    }
