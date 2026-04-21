using System.Net;
using TestTrackingDiagrams.Extensions.SQS;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.SQS;

public class SqsTrackingMessageHandlerTests : IDisposable
{
    private class StubInnerHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedRequestBody { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"MessageId": "msg-123"}""")
        };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            if (request.Content is not null)
                CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return ResponseToReturn;
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

    private HttpMessageInvoker CreateInvoker(SqsTrackingMessageHandlerOptions options)
    {
        var handler = new SqsTrackingMessageHandler(options, _innerHandler);
        return new HttpMessageInvoker(handler);
    }

    private SqsTrackingMessageHandlerOptions MakeOptions(
        SqsTrackingVerbosity verbosity = SqsTrackingVerbosity.Detailed,
        string serviceName = "SQS",
        string callerName = "TestCaller") => new()
    {
        ServiceName = serviceName,
        CallingServiceName = callerName,
        Verbosity = verbosity,
        CurrentTestInfoFetcher = () => ("My Test", _testId),
    };

    private static HttpRequestMessage MakeSendMessageRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://sqs.us-east-1.amazonaws.com/123456789012/orders-queue");
        request.Headers.Add("X-Amz-Target", "AmazonSQS.SendMessage");
        request.Content = new StringContent(
            """{"QueueUrl": "https://sqs.us-east-1.amazonaws.com/123456789012/orders-queue", "MessageBody": "hello"}""",
            System.Text.Encoding.UTF8, "application/x-amz-json-1.0");
        return request;
    }

    private static HttpRequestMessage MakeReceiveMessageRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://sqs.us-east-1.amazonaws.com/123456789012/orders-queue");
        request.Headers.Add("X-Amz-Target", "AmazonSQS.ReceiveMessage");
        request.Content = new StringContent(
            """{"QueueUrl": "https://sqs.us-east-1.amazonaws.com/123456789012/orders-queue", "MaxNumberOfMessages": 10}""",
            System.Text.Encoding.UTF8, "application/x-amz-json-1.0");
        return request;
    }

    private static HttpRequestMessage MakeDeleteMessageRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://sqs.us-east-1.amazonaws.com/123456789012/orders-queue");
        request.Headers.Add("X-Amz-Target", "AmazonSQS.DeleteMessage");
        request.Content = new StringContent(
            """{"QueueUrl": "https://sqs.us-east-1.amazonaws.com/123456789012/orders-queue", "ReceiptHandle": "abc"}""",
            System.Text.Encoding.UTF8, "application/x-amz-json-1.0");
        return request;
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

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public async Task Logs_correct_service_and_caller_names()
    {
        using var invoker = CreateInvoker(MakeOptions(callerName: "MyApi", serviceName: "OrdersSQS"));

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal("OrdersSQS", logs[0].ServiceName);
        Assert.Equal("MyApi", logs[0].CallerName);
    }

    [Fact]
    public async Task Does_not_log_when_no_test_info_fetcher()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task Request_is_still_forwarded_when_no_test_info_fetcher()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        Assert.NotNull(_innerHandler.CapturedRequest);
    }

    // ─── Request body reconstruction ──────────────────────────

    [Fact]
    public async Task Request_body_is_reconstructed_after_classification()
    {
        using var invoker = CreateInvoker(MakeOptions());

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        Assert.NotNull(_innerHandler.CapturedRequestBody);
        Assert.Contains("orders-queue", _innerHandler.CapturedRequestBody);
    }

    [Fact]
    public async Task Request_body_is_reconstructed_even_when_no_test_info()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        Assert.NotNull(_innerHandler.CapturedRequestBody);
        Assert.Contains("orders-queue", _innerHandler.CapturedRequestBody);
    }

    // ─── Detailed verbosity ────────────────────────────────────

    [Fact]
    public async Task Detailed_SendMessage_UsesClassifiedLabel()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("SendMessage", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Detailed_ReceiveMessage_UsesClassifiedLabel()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeReceiveMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("ReceiveMessage", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Detailed_UsesSqsUriScheme()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.StartsWith("sqs://", log.Uri.ToString());
    }

    [Fact]
    public async Task Detailed_IncludesQueueNameInUri()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("orders-queue", log.Uri.ToString());
    }

    [Fact]
    public async Task Detailed_IncludesRequestBody()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("orders-queue", log.Content!);
    }

    [Fact]
    public async Task Detailed_IncludesResponseContent()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Detailed));

        await invoker.SendAsync(MakeReceiveMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Contains("msg-123", log.Content!);
    }

    // ─── Summarised verbosity ──────────────────────────────────

    [Fact]
    public async Task Summarised_UsesOperationNameOnly()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("SendMessage", log.Method.Value?.ToString());
    }

    [Fact]
    public async Task Summarised_OmitsRequestContent()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Null(log.Content);
    }

    [Fact]
    public async Task Summarised_OmitsResponseContent()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeReceiveMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Null(log.Content);
    }

    [Fact]
    public async Task Summarised_OmitsHeaders()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Summarised));

        await invoker.SendAsync(MakeReceiveMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Empty(log.Headers);
    }

    [Fact]
    public async Task Summarised_SkipsOtherOperations()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Summarised));

        // No X-Amz-Target header → Other
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        await invoker.SendAsync(request, CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    // ─── Raw verbosity ────────────────────────────────────────

    [Fact]
    public async Task Raw_UsesHttpMethodAsMethod()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal(HttpMethod.Post, log.Method.Value);
    }

    [Fact]
    public async Task Raw_IncludesFullContent()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("orders-queue", log.Content!);
    }

    [Fact]
    public async Task Raw_UsesOriginalUri()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Raw));

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("amazonaws.com", log.Uri.ToString());
    }

    [Fact]
    public async Task Raw_DoesNotSkipOtherOperations()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Raw));

        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        await invoker.SendAsync(request, CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    // ─── Header filtering ─────────────────────────────────────

    [Fact]
    public async Task Detailed_ExcludesDefaultNoisyHeaders()
    {
        using var invoker = CreateInvoker(MakeOptions(SqsTrackingVerbosity.Detailed));
        var request = MakeSendMessageRequest();
        request.Headers.Add("x-amz-date", "20240101T000000Z");
        request.Headers.Add("x-custom-header", "keep-me");

        await invoker.SendAsync(request, CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.DoesNotContain(log.Headers, h => h.Key == "x-amz-date");
        Assert.Contains(log.Headers, h => h.Key == "x-custom-header");
    }

    // ─── Status code ──────────────────────────────────────────

    [Fact]
    public async Task Response_IncludesStatusCode()
    {
        _innerHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };
        using var invoker = CreateInvoker(MakeOptions());

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal(HttpStatusCode.OK, log.StatusCode?.Value);
    }

    // ─── ITrackingComponent ────────────────────────────────────

    [Fact]
    public void Implements_ITrackingComponent()
    {
        var handler = new SqsTrackingMessageHandler(MakeOptions());
        Assert.IsAssignableFrom<ITrackingComponent>(handler);
    }

    [Fact]
    public void WasInvoked_IsFalse_BeforeAnyRequests()
    {
        var handler = new SqsTrackingMessageHandler(MakeOptions());
        Assert.False(handler.WasInvoked);
    }

    [Fact]
    public async Task WasInvoked_IsTrue_AfterRequest()
    {
        var handler = new SqsTrackingMessageHandler(MakeOptions(), _innerHandler);
        using var invoker = new HttpMessageInvoker(handler);
        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);

        Assert.True(handler.WasInvoked);
    }

    [Fact]
    public void InvocationCount_StartsAtZero()
    {
        var handler = new SqsTrackingMessageHandler(MakeOptions());
        Assert.Equal(0, handler.InvocationCount);
    }

    [Fact]
    public async Task InvocationCount_IncreasesWithEachCall()
    {
        var handler = new SqsTrackingMessageHandler(MakeOptions(), _innerHandler);
        using var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(MakeSendMessageRequest(), CancellationToken.None);
        await invoker.SendAsync(MakeReceiveMessageRequest(), CancellationToken.None);
        await invoker.SendAsync(MakeDeleteMessageRequest(), CancellationToken.None);

        Assert.Equal(3, handler.InvocationCount);
    }

    [Fact]
    public void ComponentName_MatchesServiceName()
    {
        var handler = new SqsTrackingMessageHandler(MakeOptions(serviceName: "MySQS"));
        Assert.Equal("SqsTrackingMessageHandler (MySQS)", handler.ComponentName);
    }

    [Fact]
    public void Constructor_AutoRegistersWithTrackingComponentRegistry()
    {
        TrackingComponentRegistry.Clear();
        var handler = new SqsTrackingMessageHandler(MakeOptions());

        var components = TrackingComponentRegistry.GetRegisteredComponents();
        Assert.Contains(components, c => ReferenceEquals(c, handler));
    }
}
