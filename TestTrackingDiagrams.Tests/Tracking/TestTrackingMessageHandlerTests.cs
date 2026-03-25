using System.Net;
using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Constants;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

public class TestTrackingMessageHandlerTests : IDisposable
{
    // ─── Test infrastructure ────────────────────────────────────

    /// <summary>
    /// A simple inner handler that captures the request and returns a canned response.
    /// This replaces the real HTTP call so we can inspect what the handler does.
    /// </summary>
    private class StubInnerHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK) { Content = new StringContent("response-body") };

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

    private HttpMessageInvoker CreateInvoker(TestTrackingMessageHandlerOptions options, IHttpContextAccessor? httpContextAccessor = null)
    {
        var handler = new TestTrackingMessageHandler(options, httpContextAccessor)
        {
            InnerHandler = _innerHandler
        };
        return new HttpMessageInvoker(handler);
    }

    private TestTrackingMessageHandlerOptions DefaultOptions(
        string callerName = "TestCaller",
        string? fixedServiceName = "TargetService") => new()
    {
        CallingServiceName = callerName,
        FixedNameForReceivingService = fixedServiceName,
        CurrentTestInfoFetcher = () => ("My Test", _testId),
    };

    private static HttpRequestMessage MakeGetRequest(string url = "http://target-service:5000/api/items")
    {
        return new HttpRequestMessage(HttpMethod.Get, url);
    }

    public void Dispose()
    {
        _innerHandler.Dispose();
    }

    // ─── Basic request/response logging ─────────────────────────

    [Fact]
    public async Task Logs_both_a_request_and_a_response_for_each_call()
    {
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public async Task Request_log_contains_correct_method_and_uri()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        var request = new HttpRequestMessage(HttpMethod.Post, "http://target-service:5000/api/items");

        await invoker.SendAsync(request, CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal(HttpMethod.Post, requestLog.Method.Value);
        Assert.Equal(new Uri("http://target-service:5000/api/items"), requestLog.Uri);
    }

    [Fact]
    public async Task Request_log_captures_request_body()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        var request = new HttpRequestMessage(HttpMethod.Post, "http://target-service:5000/api/items")
        {
            Content = new StringContent("request-body-content")
        };

        await invoker.SendAsync(request, CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("request-body-content", requestLog.Content);
    }

    [Fact]
    public async Task Request_log_has_null_content_when_no_body()
    {
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Null(requestLog.Content);
    }

    [Fact]
    public async Task Response_log_captures_response_body()
    {
        _innerHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("custom-response-body")
        };
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var responseLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal("custom-response-body", responseLog.Content);
    }

    [Fact]
    public async Task Response_log_captures_status_code()
    {
        _innerHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("")
        };
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var responseLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal(HttpStatusCode.NotFound, responseLog.StatusCode!.Value);
    }

    [Fact]
    public async Task Response_log_captures_response_headers()
    {
        _innerHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("")
        };
        _innerHandler.ResponseToReturn.Headers.Add("X-Custom-Response", "resp-value");
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var responseLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Contains(responseLog.Headers, h => h.Key == "X-Custom-Response" && h.Value == "resp-value");
    }

    // ─── Test info from CurrentTestInfoFetcher ──────────────────

    [Fact]
    public async Task Logs_test_name_and_id_from_current_test_info_fetcher()
    {
        var options = new TestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "Svc",
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Fetched Test Name", _testId),
        };
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Fetched Test Name", requestLog.TestName);
        Assert.Equal(_testId, requestLog.TestId);
    }

    // ─── Service name resolution ────────────────────────────────

    [Fact]
    public async Task Uses_fixed_service_name_when_configured()
    {
        var options = DefaultOptions(fixedServiceName: "MyFixedService");
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("MyFixedService", requestLog.ServiceName);
    }

    [Fact]
    public async Task Uses_port_to_service_name_mapping_when_no_fixed_name()
    {
        var options = new TestTrackingMessageHandlerOptions
        {
            PortsToServiceNames = new Dictionary<int, string> { { 5000, "MappedService" } },
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Test", _testId),
        };
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/api"), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("MappedService", requestLog.ServiceName);
    }

    [Fact]
    public async Task Falls_back_to_localhost_port_when_port_not_in_mapping()
    {
        var options = new TestTrackingMessageHandlerOptions
        {
            PortsToServiceNames = new Dictionary<int, string> { { 9999, "Other" } },
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Test", _testId),
        };
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost:7777/api"), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("localhost:7777", requestLog.ServiceName);
    }

    // ─── Caller name ────────────────────────────────────────────

    [Fact]
    public async Task Logs_calling_service_name_from_options()
    {
        var options = DefaultOptions(callerName: "MyApp");
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("MyApp", requestLog.CallerName);
    }

    // ─── TraceId generation and propagation ─────────────────────

    [Fact]
    public async Task Adds_trace_id_header_to_outgoing_request()
    {
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        Assert.True(_innerHandler.CapturedRequest!.Headers.Contains(TestTrackingHttpHeaders.TraceIdHeader));
    }

    [Fact]
    public async Task Request_and_response_logs_share_the_same_trace_id()
    {
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(logs[0].TraceId, logs[1].TraceId);
    }

    [Fact]
    public async Task Request_and_response_logs_share_the_same_request_response_id()
    {
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(logs[0].RequestResponseId, logs[1].RequestResponseId);
    }

    // ─── Tracking headers added to outgoing request ─────────────

    [Fact]
    public async Task Adds_test_name_header_to_outgoing_request()
    {
        var options = new TestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "Svc",
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Test Name Here", "id-42"),
        };
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var headerValue = _innerHandler.CapturedRequest!.Headers.GetValues(TestTrackingHttpHeaders.CurrentTestNameHeader).Single();
        Assert.Equal("Test Name Here", headerValue);
    }

    [Fact]
    public async Task Adds_test_id_header_to_outgoing_request()
    {
        var options = new TestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "Svc",
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Test", "my-test-id-99"),
        };
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var headerValue = _innerHandler.CapturedRequest!.Headers.GetValues(TestTrackingHttpHeaders.CurrentTestIdHeader).Single();
        Assert.Equal("my-test-id-99", headerValue);
    }

    [Fact]
    public async Task Adds_caller_name_header_to_outgoing_request()
    {
        var options = DefaultOptions(callerName: "OriginApp");
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var headerValue = _innerHandler.CapturedRequest!.Headers.GetValues(TestTrackingHttpHeaders.CallerNameHeader).Single();
        Assert.Equal("OriginApp", headerValue);
    }

    // ─── Tracking ignore header ─────────────────────────────────

    [Fact]
    public async Task Request_without_ignore_header_has_tracking_ignore_false()
    {
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.False(requestLog.TrackingIgnore);
    }

    [Fact]
    public async Task Request_with_ignore_header_has_tracking_ignore_true()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        var request = MakeGetRequest();
        request.Headers.Add(TestTrackingHttpHeaders.Ignore, "true");

        await invoker.SendAsync(request, CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.True(requestLog.TrackingIgnore);
    }

    [Fact]
    public async Task Response_also_gets_tracking_ignore_from_request_headers()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        var request = MakeGetRequest();
        request.Headers.Add(TestTrackingHttpHeaders.Ignore, "true");

        await invoker.SendAsync(request, CancellationToken.None);

        var responseLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.True(responseLog.TrackingIgnore);
    }

    // ─── Request headers captured in log ────────────────────────

    [Fact]
    public async Task Request_log_captures_original_request_headers()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        var request = MakeGetRequest();
        request.Headers.Add("X-Custom", "my-value");

        await invoker.SendAsync(request, CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains(requestLog.Headers, h => h.Key == "X-Custom" && h.Value == "my-value");
    }

    // ─── HttpContextAccessor: headers from incoming context ─────

    private static IHttpContextAccessor CreateHttpContextAccessor(params (string Key, string Value)[] headers)
    {
        var context = new DefaultHttpContext();
        foreach (var (key, value) in headers)
        {
            context.Request.Headers[key] = value;
        }
        return new HttpContextAccessor { HttpContext = context };
    }

    [Fact]
    public async Task Uses_test_name_from_http_context_when_header_present()
    {
        var accessor = CreateHttpContextAccessor(
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Context Test Name"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, _testId));
        var options = DefaultOptions();
        using var invoker = CreateInvoker(options, accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Context Test Name", requestLog.TestName);
        Assert.Equal(_testId, requestLog.TestId);
    }

    [Fact]
    public async Task Uses_trace_id_from_http_context_when_header_present()
    {
        var traceGuid = Guid.NewGuid();
        var accessor = CreateHttpContextAccessor(
            (TestTrackingHttpHeaders.TraceIdHeader, traceGuid.ToString()),
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Test"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, _testId));
        using var invoker = CreateInvoker(DefaultOptions(), accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal(traceGuid, requestLog.TraceId);
    }

    [Fact]
    public async Task Does_not_add_trace_id_header_when_already_present_in_context()
    {
        var traceGuid = Guid.NewGuid();
        var accessor = CreateHttpContextAccessor(
            (TestTrackingHttpHeaders.TraceIdHeader, traceGuid.ToString()),
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Test"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, _testId));
        using var invoker = CreateInvoker(DefaultOptions(), accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        // The header should be present (forwarded), but only one value
        var traceHeaders = _innerHandler.CapturedRequest!.Headers
            .Where(h => h.Key == TestTrackingHttpHeaders.TraceIdHeader)
            .SelectMany(h => h.Value)
            .ToList();

        // Should NOT have added a duplicate — the handler skips adding when hasTraceIdHeader is true
        Assert.DoesNotContain(traceHeaders, v => v != traceGuid.ToString());
    }

    [Fact]
    public async Task Uses_caller_name_from_http_context_when_header_present()
    {
        var accessor = CreateHttpContextAccessor(
            (TestTrackingHttpHeaders.CallerNameHeader, "UpstreamCaller"),
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Test"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, _testId));
        using var invoker = CreateInvoker(DefaultOptions(), accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        // When caller name header is present, it does NOT add a new one to the outgoing request
        // (the hasCallerNameHeader flag suppresses the add)
        var callerHeaders = _innerHandler.CapturedRequest!.Headers
            .Where(h => h.Key == TestTrackingHttpHeaders.CallerNameHeader)
            .SelectMany(h => h.Value)
            .ToList();
        Assert.DoesNotContain("TestCaller", callerHeaders);
    }

    [Fact]
    public async Task Without_http_context_accessor_uses_fetcher_and_generates_trace_id()
    {
        var options = new TestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "Svc",
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Fetcher Test", _testId),
        };
        using var invoker = CreateInvoker(options, httpContextAccessor: null);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Fetcher Test", requestLog.TestName);
        Assert.Equal(_testId, requestLog.TestId);
        Assert.NotEqual(Guid.Empty, requestLog.TraceId);
    }

    // ─── Header forwarding ──────────────────────────────────────

    [Fact]
    public async Task Forwards_configured_headers_from_http_context_to_outgoing_request()
    {
        var accessor = CreateHttpContextAccessor(
            ("X-Correlation-Id", "corr-123"),
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Test"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, "id-1"));
        var options = new TestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "Svc",
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Test", "id-1"),
            HeadersToForward = ["X-Correlation-Id"],
        };
        using var invoker = CreateInvoker(options, accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var forwardedValue = _innerHandler.CapturedRequest!.Headers.GetValues("X-Correlation-Id").Single();
        Assert.Equal("corr-123", forwardedValue);
    }

    [Fact]
    public async Task Does_not_forward_headers_that_are_not_in_context()
    {
        var accessor = CreateHttpContextAccessor(
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Test"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, "id-1"));
        var options = new TestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "Svc",
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Test", "id-1"),
            HeadersToForward = ["X-Missing-Header"],
        };
        using var invoker = CreateInvoker(options, accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        Assert.False(_innerHandler.CapturedRequest!.Headers.Contains("X-Missing-Header"));
    }

    [Fact]
    public async Task Does_not_forward_headers_when_no_http_context_accessor()
    {
        var options = new TestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "Svc",
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Test", "id-1"),
            HeadersToForward = ["X-Forward-Me"],
        };
        using var invoker = CreateInvoker(options, httpContextAccessor: null);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        Assert.False(_innerHandler.CapturedRequest!.Headers.Contains("X-Forward-Me"));
    }

    [Fact]
    public async Task Does_not_forward_headers_when_empty_forwarding_list()
    {
        var accessor = CreateHttpContextAccessor(
            ("X-Something", "val"),
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Test"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, "id-1"));
        var options = new TestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "Svc",
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Test", "id-1"),
            HeadersToForward = [],
        };
        using var invoker = CreateInvoker(options, accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        Assert.False(_innerHandler.CapturedRequest!.Headers.Contains("X-Something"));
    }

    // ─── Multiple concurrent requests ───────────────────────────

    [Fact]
    public async Task Multiple_requests_each_get_their_own_request_response_id()
    {
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        var firstPairId = logs[0].RequestResponseId;
        var secondPairId = logs[2].RequestResponseId;
        Assert.NotEqual(firstPairId, secondPairId);
    }

    // ─── Return value ───────────────────────────────────────────

    [Fact]
    public async Task Returns_the_response_from_the_inner_handler()
    {
        _innerHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("created-item")
        };
        using var invoker = CreateInvoker(DefaultOptions());

        var response = await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("created-item", body);
    }

    // ─── Context headers suppress adding new ones ───────────────

    [Fact]
    public async Task Does_not_add_test_name_header_when_already_in_context()
    {
        var accessor = CreateHttpContextAccessor(
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Existing Name"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, _testId));
        using var invoker = CreateInvoker(DefaultOptions(), accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        // When context has the header, the handler skips adding it to the outgoing request.
        // The header from context is NOT auto-forwarded to HttpRequestMessage headers
        // (unless explicitly listed in HeadersToForward), so it won't appear.
        Assert.False(_innerHandler.CapturedRequest!.Headers.Contains(TestTrackingHttpHeaders.CurrentTestNameHeader));

        // But the log still uses the value from context
        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Existing Name", requestLog.TestName);
    }

    [Fact]
    public async Task Does_not_add_test_id_header_when_already_in_context()
    {
        var accessor = CreateHttpContextAccessor(
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Test"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, _testId));
        using var invoker = CreateInvoker(DefaultOptions(), accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        // Same as test name: context header suppresses adding, but isn't auto-forwarded
        Assert.False(_innerHandler.CapturedRequest!.Headers.Contains(TestTrackingHttpHeaders.CurrentTestIdHeader));

        // But the log still uses the value from context
        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal(_testId, requestLog.TestId);
    }

    // ─── Port-based service name with multiple mappings ─────────

    [Fact]
    public async Task Resolves_different_service_names_for_different_ports()
    {
        var options = new TestTrackingMessageHandlerOptions
        {
            PortsToServiceNames = new Dictionary<int, string>
            {
                { 5001, "ServiceA" },
                { 5002, "ServiceB" },
            },
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Test", _testId),
        };

        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost:5001/api"), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("ServiceA", requestLog.ServiceName);
    }

    // ─── Both request and response reference the same service ───

    [Fact]
    public async Task Request_and_response_logs_reference_same_service_and_caller()
    {
        var options = DefaultOptions(callerName: "MyCaller", fixedServiceName: "MyService");
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.All(logs, log =>
        {
            Assert.Equal("MyService", log.ServiceName);
            Assert.Equal("MyCaller", log.CallerName);
        });
    }

    // ─── Response logs same URI and method as request ────────────

    [Fact]
    public async Task Response_log_has_same_uri_and_method_as_request()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        var request = new HttpRequestMessage(HttpMethod.Put, "http://target-service:5000/api/items/42");

        await invoker.SendAsync(request, CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(logs[0].Uri, logs[1].Uri);
        Assert.Equal(logs[0].Method.Value, logs[1].Method.Value);
    }

    // ─── Forwarding multiple headers ────────────────────────────

    [Fact]
    public async Task Forwards_multiple_configured_headers()
    {
        var accessor = CreateHttpContextAccessor(
            ("X-Header-A", "val-a"),
            ("X-Header-B", "val-b"),
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Test"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, "id-1"));
        var options = new TestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "Svc",
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Test", "id-1"),
            HeadersToForward = ["X-Header-A", "X-Header-B"],
        };
        using var invoker = CreateInvoker(options, accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        Assert.Equal("val-a", _innerHandler.CapturedRequest!.Headers.GetValues("X-Header-A").Single());
        Assert.Equal("val-b", _innerHandler.CapturedRequest!.Headers.GetValues("X-Header-B").Single());
    }

    // ─── HttpContextAccessor with null HttpContext ───────────────

    [Fact]
    public async Task Handles_http_context_accessor_with_null_context_gracefully()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var options = new TestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "Svc",
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Test", _testId),
            HeadersToForward = ["X-Something"],
        };
        using var invoker = CreateInvoker(options, accessor);

        // Should not throw — both SendAsync and ForwardHeaders now null-check HttpContext
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        Assert.False(_innerHandler.CapturedRequest!.Headers.Contains("X-Something"));
        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Test", requestLog.TestName);
    }

    // ─── Synchronous Send() method ──────────────────────────────

    [Fact]
    public void Sync_send_logs_request_and_response()
    {
        var handler = new TestTrackingMessageHandler(DefaultOptions())
        {
            InnerHandler = _innerHandler
        };
        using var client = new HttpClient(handler);

        // HttpClient.Send uses the synchronous Send() override, which delegates to SendAsync
        var response = client.Send(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── Request headers snapshot taken before tracking headers ──

    [Fact]
    public async Task Logged_request_headers_do_not_include_injected_tracking_headers()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        var request = MakeGetRequest();
        request.Headers.Add("X-Original", "original-value");

        await invoker.SendAsync(request, CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);

        // The original header should be logged
        Assert.Contains(requestLog.Headers, h => h.Key == "X-Original" && h.Value == "original-value");

        // The tracking headers injected by the handler should NOT be in the logged headers
        // because the snapshot is taken before they're added
        Assert.DoesNotContain(requestLog.Headers, h => h.Key == TestTrackingHttpHeaders.TraceIdHeader);
        Assert.DoesNotContain(requestLog.Headers, h => h.Key == TestTrackingHttpHeaders.CurrentTestNameHeader);
        Assert.DoesNotContain(requestLog.Headers, h => h.Key == TestTrackingHttpHeaders.CurrentTestIdHeader);
        Assert.DoesNotContain(requestLog.Headers, h => h.Key == TestTrackingHttpHeaders.CallerNameHeader);
    }

    // ─── Multi-value headers ────────────────────────────────────

    [Fact]
    public async Task Multi_value_request_header_is_flattened_to_separate_entries()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        var request = MakeGetRequest();
        request.Headers.Add("Accept", ["text/html", "application/json"]);

        await invoker.SendAsync(request, CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        var acceptHeaders = requestLog.Headers.Where(h => h.Key == "Accept").ToList();
        Assert.Equal(2, acceptHeaders.Count);
        Assert.Contains(acceptHeaders, h => h.Value == "text/html");
        Assert.Contains(acceptHeaders, h => h.Value == "application/json");
    }

    [Fact]
    public async Task Multi_value_response_header_is_flattened_to_separate_entries()
    {
        _innerHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("")
        };
        _innerHandler.ResponseToReturn.Headers.Add("X-Multi", ["val-1", "val-2"]);
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var responseLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        var multiHeaders = responseLog.Headers.Where(h => h.Key == "X-Multi").ToList();
        Assert.Equal(2, multiHeaders.Count);
        Assert.Contains(multiHeaders, h => h.Value == "val-1");
        Assert.Contains(multiHeaders, h => h.Value == "val-2");
    }

    // ─── Ignore header on response does not affect TrackingIgnore ──

    [Fact]
    public async Task Ignore_header_on_response_only_does_not_set_tracking_ignore()
    {
        _innerHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("")
        };
        _innerHandler.ResponseToReturn.Headers.Add(TestTrackingHttpHeaders.Ignore, "true");
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        // TrackingIgnore is derived from request headers, not response headers
        var responseLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.False(responseLog.TrackingIgnore);
    }

    // ─── Partial context headers (only name, no ID) ─────────────

    [Fact]
    public async Task Context_with_test_name_but_no_test_id_throws_because_fetcher_assumes_both()
    {
        // When hasCurrentTestNameHeader is true, the handler replaces the test info fetcher
        // with one that reads both name and ID from context headers. If only the name header
        // is present, the ID StringValues is empty and .First() throws.
        var accessor = CreateHttpContextAccessor(
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Context Name Only"));
        var options = new TestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "Svc",
            CallingServiceName = "Caller",
            CurrentTestInfoFetcher = () => ("Fetcher Name", "fetcher-id"),
        };
        using var invoker = CreateInvoker(options, accessor);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => invoker.SendAsync(MakeGetRequest(), CancellationToken.None));
    }

    // ─── Fixed service name ignores port completely ─────────────

    [Fact]
    public async Task Fixed_service_name_is_used_regardless_of_port()
    {
        var options = DefaultOptions(fixedServiceName: "AlwaysThisService");

        using var invoker1 = CreateInvoker(options);
        await invoker1.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost:3000/api"), CancellationToken.None);

        using var invoker2 = CreateInvoker(options);
        await invoker2.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost:9999/api"), CancellationToken.None);

        var logs = GetLogsFromThisTest().Where(l => l.Type == RequestResponseType.Request).ToList();
        Assert.All(logs, l => Assert.Equal("AlwaysThisService", l.ServiceName));
    }

    // ─── Implicit setup step detection ──────────────────────────

    private TestTrackingMessageHandlerOptions OptionsWithStepFetcher(Func<string?> stepTypeFetcher) => new()
    {
        CallingServiceName = "Caller",
        FixedNameForReceivingService = "Svc",
        CurrentTestInfoFetcher = () => ("Test", _testId),
        CurrentStepTypeFetcher = stepTypeFetcher,
    };

    [Fact]
    public async Task Injects_IsActionStart_marker_when_step_transitions_from_GIVEN_to_WHEN()
    {
        var currentStep = "GIVEN";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "WHEN";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Contains(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task No_IsActionStart_marker_when_no_step_type_fetcher()
    {
        var options = DefaultOptions();
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.DoesNotContain(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task No_IsActionStart_marker_when_all_steps_are_GIVEN()
    {
        var options = OptionsWithStepFetcher(() => "GIVEN");
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.DoesNotContain(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task IsActionStart_marker_injected_before_first_WHEN_request()
    {
        var currentStep = "GIVEN";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "WHEN";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        var markerIndex = Array.FindIndex(logs, l => l.IsActionStart);
        var whenRequestIndex = Array.FindIndex(logs, markerIndex + 1, l => l.Type == RequestResponseType.Request && !l.IsActionStart);
        Assert.True(markerIndex < whenRequestIndex, "IsActionStart marker should appear before the WHEN request");
    }

    [Fact]
    public async Task Only_one_IsActionStart_marker_injected_across_multiple_WHEN_requests()
    {
        var currentStep = "GIVEN";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "WHEN";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Single(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task AND_steps_after_GIVEN_do_not_trigger_IsActionStart()
    {
        var currentStep = "GIVEN";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "AND";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.DoesNotContain(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task WHEN_after_AND_given_triggers_IsActionStart()
    {
        var currentStep = "GIVEN";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "AND";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "WHEN";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Contains(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task Step_type_detection_is_case_insensitive()
    {
        var currentStep = "given";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "when";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Contains(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task No_IsActionStart_when_first_step_is_WHEN_without_prior_GIVEN()
    {
        var options = OptionsWithStepFetcher(() => "WHEN");
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.DoesNotContain(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task THEN_after_GIVEN_triggers_IsActionStart()
    {
        var currentStep = "GIVEN";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "THEN";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Contains(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task BUT_steps_after_GIVEN_do_not_trigger_IsActionStart()
    {
        var currentStep = "GIVEN";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "BUT";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.DoesNotContain(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task Null_step_type_does_not_trigger_IsActionStart()
    {
        string? currentStep = "GIVEN";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = null;
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.DoesNotContain(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task Multiple_GIVEN_requests_before_WHEN_produces_single_marker()
    {
        var currentStep = "GIVEN";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "WHEN";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Single(logs, l => l.IsActionStart);
    }

    [Fact]
    public async Task Full_GIVEN_AND_WHEN_THEN_flow_injects_marker_before_WHEN()
    {
        var currentStep = "GIVEN";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "AND";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "WHEN";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "THEN";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        // 4 requests + 4 responses + 1 action start marker = 9
        Assert.Equal(9, logs.Length);
        Assert.Single(logs, l => l.IsActionStart);
        var markerIndex = Array.FindIndex(logs, l => l.IsActionStart);
        // GIVEN request(0) + response(1) + AND request(2) + response(3) + marker(4) + WHEN request(5) ...
        Assert.Equal(4, markerIndex);
    }

    [Fact]
    public async Task IsActionStart_marker_has_correct_test_id()
    {
        var currentStep = "GIVEN";
        var options = OptionsWithStepFetcher(() => currentStep);
        using var invoker = CreateInvoker(options);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        currentStep = "WHEN";
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var marker = GetLogsFromThisTest().Single(l => l.IsActionStart);
        Assert.Equal(_testId, marker.TestId);
    }

    [Fact]
    public async Task No_implicit_detection_on_server_side_when_http_context_has_test_headers()
    {
        var stepFetcherCalled = false;
        var options = new TestTrackingMessageHandlerOptions
        {
            CallingServiceName = "Caller",
            FixedNameForReceivingService = "Svc",
            CurrentTestInfoFetcher = () => ("Test", _testId),
            CurrentStepTypeFetcher = () =>
            {
                stepFetcherCalled = true;
                return "WHEN";
            },
        };
        var accessor = CreateHttpContextAccessor(
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Server Test"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, _testId));
        using var invoker = CreateInvoker(options, accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        Assert.False(stepFetcherCalled, "CurrentStepTypeFetcher should not be called on the server side");
        Assert.DoesNotContain(GetLogsFromThisTest(), l => l.IsActionStart);
    }

    [Fact]
    public async Task Step_fetcher_that_throws_does_not_crash_when_server_side_headers_present()
    {
        var options = new TestTrackingMessageHandlerOptions
        {
            CallingServiceName = "Caller",
            FixedNameForReceivingService = "Svc",
            CurrentTestInfoFetcher = () => ("Test", _testId),
            CurrentStepTypeFetcher = () => throw new InvalidOperationException("Not in scenario context"),
        };
        var accessor = CreateHttpContextAccessor(
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Server Test"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, _testId));
        using var invoker = CreateInvoker(options, accessor);

        // Should NOT throw — the fetcher is never called on the server side
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length); // request + response, no crash
    }

    // ─── DiagramFocus propagation ───────────────────────────────

    [Fact]
    public async Task Request_FocusFields_populated_when_DiagramFocus_Request_is_set()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        DiagramFocus.Request<SamplePayload>(x => x.Name, x => x.Age);

        await invoker.SendAsync(MakePostRequest("""{"name":"Alice","age":30}"""), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.NotNull(requestLog.FocusFields);
        Assert.Contains("Name", requestLog.FocusFields);
        Assert.Contains("Age", requestLog.FocusFields);
    }

    [Fact]
    public async Task Response_FocusFields_populated_when_DiagramFocus_Response_is_set()
    {
        _innerHandler.ResponseToReturn = new(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":"123","status":"ok"}""")
        };
        using var invoker = CreateInvoker(DefaultOptions());
        DiagramFocus.Response<SampleResult>(x => x.Status);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var responseLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.NotNull(responseLog.FocusFields);
        Assert.Single(responseLog.FocusFields);
        Assert.Equal("Status", responseLog.FocusFields[0]);
    }

    [Fact]
    public async Task FocusFields_is_null_when_no_focus_set()
    {
        using var invoker = CreateInvoker(DefaultOptions());

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var logs = GetLogsFromThisTest();
        Assert.All(logs, l => Assert.Null(l.FocusFields));
    }

    [Fact]
    public async Task Focus_is_consumed_after_one_HTTP_call()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        DiagramFocus.Request<SamplePayload>(x => x.Name);
        DiagramFocus.Response<SampleResult>(x => x.Status);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);
        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var secondRequestLog = GetLogsFromThisTest()
            .Where(l => l.Type == RequestResponseType.Request).Skip(1).First();
        var secondResponseLog = GetLogsFromThisTest()
            .Where(l => l.Type == RequestResponseType.Response).Skip(1).First();
        Assert.Null(secondRequestLog.FocusFields);
        Assert.Null(secondResponseLog.FocusFields);
    }

    [Fact]
    public async Task Focus_not_consumed_by_downstream_handler_when_HttpContext_is_set()
    {
        DiagramFocus.Request<SamplePayload>(x => x.Name);
        DiagramFocus.Response<SampleResult>(x => x.Status);

        var accessor = CreateHttpContextAccessor(
            (TestTrackingHttpHeaders.CurrentTestNameHeader, "Server Test"),
            (TestTrackingHttpHeaders.CurrentTestIdHeader, _testId));
        using var invoker = CreateInvoker(DefaultOptions(), accessor);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        // When HttpContext is set, the handler is on the server side — focus should NOT be consumed
        var logs = GetLogsFromThisTest();
        Assert.All(logs, l => Assert.Null(l.FocusFields));

        // And focus should still be pending (not consumed)
        Assert.NotNull(DiagramFocus.ConsumePendingRequestFocus());
        Assert.NotNull(DiagramFocus.ConsumePendingResponseFocus());
    }

    [Fact]
    public async Task Both_request_and_response_focus_set_independently()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        DiagramFocus.Request<SamplePayload>(x => x.Name);
        DiagramFocus.Response<SampleResult>(x => x.Id, x => x.Status);

        await invoker.SendAsync(MakeGetRequest(), CancellationToken.None);

        var requestLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        var responseLog = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);

        Assert.NotNull(requestLog.FocusFields);
        Assert.Single(requestLog.FocusFields);
        Assert.Equal("Name", requestLog.FocusFields[0]);

        Assert.NotNull(responseLog.FocusFields);
        Assert.Equal(2, responseLog.FocusFields.Length);
        Assert.Contains("Id", responseLog.FocusFields);
        Assert.Contains("Status", responseLog.FocusFields);
    }

    // ─── Focus helpers ──────────────────────────────────────────

    // ReSharper disable once ClassNeverInstantiated.Local
    private record SamplePayload(string Name, int Age);
    // ReSharper disable once ClassNeverInstantiated.Local
    private record SampleResult(string Id, string Status);

    private static HttpRequestMessage MakePostRequest(string body)
    {
        return new HttpRequestMessage(HttpMethod.Post, "http://target-service:5000/api/items")
        {
            Content = new StringContent(body)
        };
    }
}
