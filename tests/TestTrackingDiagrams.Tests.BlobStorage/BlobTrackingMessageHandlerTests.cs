using System.Net;
using TestTrackingDiagrams.Extensions.BlobStorage;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.BlobStorage;

public class BlobTrackingMessageHandlerTests : IDisposable
{
    // ─── Test infrastructure ────────────────────────────────────

    private class StubInnerHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK) { Content = new StringContent("blob-content-here") };

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

    private HttpMessageInvoker CreateInvoker(BlobTrackingMessageHandlerOptions options)
    {
        var handler = new BlobTrackingMessageHandler(options, _innerHandler);
        return new HttpMessageInvoker(handler);
    }

    private BlobTrackingMessageHandlerOptions MakeOptions(
        BlobTrackingVerbosity verbosity = BlobTrackingVerbosity.Detailed,
        string serviceName = "BlobStorage",
        string callerName = "TestCaller") => new()
    {
        ServiceName = serviceName,
        CallingServiceName = callerName,
        Verbosity = verbosity,
        CurrentTestInfoFetcher = () => ("My Test", _testId),
    };

    private static HttpRequestMessage MakeUploadRequest()
    {
        return new HttpRequestMessage(HttpMethod.Put,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt")
        {
            Content = new StringContent("hello world")
        };
    }

    private static HttpRequestMessage MakeDownloadRequest()
    {
        return new HttpRequestMessage(HttpMethod.Get,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt");
    }

    private static HttpRequestMessage MakeDeleteRequest()
    {
        return new HttpRequestMessage(HttpMethod.Delete,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt");
    }

    private static HttpRequestMessage MakeListRequest()
    {
        return new HttpRequestMessage(HttpMethod.Get,
            "https://account.blob.core.windows.net/mycontainer?restype=container&comp=list");
    }

    private static HttpRequestMessage MakeCreateContainerRequest()
    {
        return new HttpRequestMessage(HttpMethod.Put,
            "https://account.blob.core.windows.net/mycontainer?restype=container");
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

        await invoker.SendAsync(MakeUploadRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public async Task Logs_correct_service_and_caller_names()
    {
        using var invoker = CreateInvoker(MakeOptions(callerName: "MyApi", serviceName: "DocumentsBlobStorage"));

        await invoker.SendAsync(MakeUploadRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal("DocumentsBlobStorage", logs[0].ServiceName);
        Assert.Equal("MyApi", logs[0].CallerName);
    }

    [Fact]
    public async Task Does_not_log_when_no_test_info_fetcher()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeUploadRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task Request_is_still_forwarded_when_no_test_info_fetcher()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeUploadRequest(), CancellationToken.None);

        Assert.NotNull(_innerHandler.CapturedRequest);
    }

    // ─── Detailed verbosity ────────────────────────────────────

    [Fact]
    public async Task Detailed_Upload_UsesClassifiedLabel()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeUploadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Upload", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Detailed_Download_UsesClassifiedLabel()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeDownloadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Download", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Detailed_IncludesResponseContent()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeDownloadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Contains("blob-content-here", log.Content);
    }

    [Fact]
    public async Task Detailed_StripsQueryParams_FromUri()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeListRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.DoesNotContain("restype", log.Uri.ToString());
        Assert.DoesNotContain("comp", log.Uri.ToString());
    }

    // ─── Summarised verbosity ──────────────────────────────────

    [Fact]
    public async Task Summarised_UsesOperationNameOnly()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeUploadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Upload", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Summarised_OmitsRequestContent()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeUploadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Null(log.Content);
    }

    [Fact]
    public async Task Summarised_OmitsResponseContent()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeDownloadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Null(log.Content);
    }

    [Fact]
    public async Task Summarised_OmitsHeaders()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeDownloadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Empty(log.Headers);
    }

    [Fact]
    public async Task Summarised_SkipsOtherOperations()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Summarised));

        // GET on container with restype=container alone → Other
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.blob.core.windows.net/mycontainer?restype=container");
        await invoker.SendAsync(request, CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    // ─── Raw verbosity ────────────────────────────────────────

    [Fact]
    public async Task Raw_UsesHttpMethodAsMethod()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeUploadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal(HttpMethod.Put, log.Method.Value);
    }

    [Fact]
    public async Task Raw_IncludesFullContent()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeUploadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("hello world", log.Content);
    }

    [Fact]
    public async Task Raw_DoesNotSkipOtherOperations()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Raw));

        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.blob.core.windows.net/mycontainer?restype=container");
        await invoker.SendAsync(request, CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    // ─── Header filtering ─────────────────────────────────────

    [Fact]
    public async Task Detailed_ExcludesDefaultNoisyHeaders()
    {
        using var invoker = CreateInvoker(MakeOptions(BlobTrackingVerbosity.Detailed));
        var request = MakeDownloadRequest();
        request.Headers.Add("x-ms-date", "Tue, 29 Mar 2016 02:03:06 GMT");
        request.Headers.Add("x-ms-custom-header", "keep-me");

        await invoker.SendAsync(request, CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.DoesNotContain(log.Headers, h => h.Key == "x-ms-date");
        Assert.Contains(log.Headers, h => h.Key == "x-ms-custom-header");
    }

    // ─── Status code ──────────────────────────────────────────

    [Fact]
    public async Task Response_IncludesStatusCode()
    {
        _innerHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("")
        };
        using var invoker = CreateInvoker(MakeOptions());

        await invoker.SendAsync(MakeUploadRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal(HttpStatusCode.Created, log.StatusCode?.Value);
    }

    // ─── ITrackingComponent ────────────────────────────────────

    [Fact]
    public void Implements_ITrackingComponent()
    {
        var handler = new BlobTrackingMessageHandler(MakeOptions());
        Assert.IsAssignableFrom<ITrackingComponent>(handler);
    }

    [Fact]
    public void WasInvoked_IsFalse_BeforeAnyRequests()
    {
        var handler = new BlobTrackingMessageHandler(MakeOptions());
        Assert.False(handler.WasInvoked);
    }

    [Fact]
    public async Task WasInvoked_IsTrue_AfterRequest()
    {
        var handler = new BlobTrackingMessageHandler(MakeOptions(), _innerHandler);
        using var invoker = new HttpMessageInvoker(handler);
        await invoker.SendAsync(MakeUploadRequest(), CancellationToken.None);

        Assert.True(handler.WasInvoked);
    }

    [Fact]
    public void InvocationCount_StartsAtZero()
    {
        var handler = new BlobTrackingMessageHandler(MakeOptions());
        Assert.Equal(0, handler.InvocationCount);
    }

    [Fact]
    public void ComponentName_MatchesServiceName()
    {
        var handler = new BlobTrackingMessageHandler(MakeOptions(serviceName: "MyBlob"));
        Assert.Equal("BlobTrackingMessageHandler (MyBlob)", handler.ComponentName);
    }

    [Fact]
    public void Constructor_AutoRegistersWithTrackingComponentRegistry()
    {
        TrackingComponentRegistry.Clear();
        var handler = new BlobTrackingMessageHandler(MakeOptions());

        var components = TrackingComponentRegistry.GetRegisteredComponents();
        Assert.Contains(components, c => ReferenceEquals(c, handler));
    }
}
