using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using TestTrackingDiagrams.Constants;

namespace TestTrackingDiagrams.Tracking;

public class TestTrackingMessageHandler : DelegatingHandler, ITrackingComponent
{
    private readonly Func<int, string> _getServiceNameFromPortTranslator;
    private readonly string? _callingServiceName;
    private readonly Func<(string Name, string Id)>? _currentTestInfoFetcher;
    private readonly Func<string?>? _currentStepTypeFetcher;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IEnumerable<string> _headersToForward;
    private string? _lastStepType;
    private bool _wasInGivenSection;
    private bool _actionStartInjected;
    private int _invocationCount;

    public TestTrackingMessageHandler(TestTrackingMessageHandlerOptions options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _getServiceNameFromPortTranslator = options.FixedNameForReceivingService is not null ? _ => options.FixedNameForReceivingService : GetPortTranslator(options.PortsToServiceNames);
        _currentTestInfoFetcher = options.CurrentTestInfoFetcher;
        _currentStepTypeFetcher = options.CurrentStepTypeFetcher;
        _callingServiceName = options.CallingServiceName;
        _httpContextAccessor = httpContextAccessor;
        _headersToForward = options.HeadersToForward;
        InnerHandler ??= new HttpClientHandler();

        InternalFlow.InternalFlowActivityListener.EnsureStarted(options.InternalFlowActivitySources);
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"TestTrackingMessageHandler ({_callingServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    private static Func<int, string> GetPortTranslator(Dictionary<int, string> serviceNamesForEachPort)
    {
        return port => serviceNamesForEachPort.TryGetValue(port, out var serviceName) ? serviceName : $"localhost:{port}";
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);

        ForwardHeaders(request);

        var requestResponseId = Guid.NewGuid();

        // Ensure trace context propagation for in-process (TestServer) scenarios.
        // Without this, the server generates a new TraceId for each request,
        // and InternalFlowSegmentBuilder cannot correlate spans.
        Activity? requestActivity = null;
        if (Activity.Current == null)
        {
            requestActivity = new Activity("TestTrackingDiagrams.Request");
            requestActivity.SetIdFormat(ActivityIdFormat.W3C);
            requestActivity.Start();
        }
        if (Activity.Current != null && !request.Headers.Contains("traceparent"))
        {
            var current = Activity.Current;
            var flags = current.Recorded ? "01" : "00";
            request.Headers.TryAddWithoutValidation("traceparent",
                $"00-{current.TraceId}-{current.SpanId}-{flags}");
        }

        var requestContentString = request.Content is null ? null : await request.Content!.ReadAsStringAsync(cancellationToken);
        var requestHeaders = request.Headers.SelectMany(x => x.Value.Select(value => (x.Key, (string?)value))).ToArray();

        StringValues currentTestNameHeaders = new();
        var hasCurrentTestNameHeader = false;

        StringValues currentTestIdHeaders = new();
        var hasCurrentTestIdHeader = false;

        StringValues callerNameHeaders = new();
        var hasCallerNameHeader = false;

        StringValues traceIdHeaders = new();
        var hasTraceIdHeader = false;

        if (_httpContextAccessor?.HttpContext is not null)
        {
            hasTraceIdHeader = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.TraceIdHeader, out traceIdHeaders);
            hasCurrentTestNameHeader = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestNameHeader, out currentTestNameHeaders);
            hasCurrentTestIdHeader = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestIdHeader, out currentTestIdHeaders);
            hasCallerNameHeader = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CallerNameHeader, out callerNameHeaders);
        }

        var currentTestInfoFetcher = hasCurrentTestNameHeader ? () => (currentTestNameHeaders.First()!, currentTestIdHeaders.First()!) : _currentTestInfoFetcher;

        var traceId = hasTraceIdHeader ? Guid.Parse(traceIdHeaders.First()!) : Guid.NewGuid();

        if (!hasTraceIdHeader)
            request.Headers.Add(TestTrackingHttpHeaders.TraceIdHeader, new[] { traceId.ToString() });

        if (!hasCurrentTestNameHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CurrentTestNameHeader, new[] { currentTestInfoFetcher!().Name });

        if (!hasCurrentTestIdHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CurrentTestIdHeader, new[] { currentTestInfoFetcher!().Id.ToString() });

        if (!hasCallerNameHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CallerNameHeader, new[] { _callingServiceName! });

        var currentTestInfo = currentTestInfoFetcher!();

        var serviceName = _getServiceNameFromPortTranslator(request.RequestUri!.Port);

        if (!hasCurrentTestNameHeader)
            InjectImplicitActionStartIfNeeded(currentTestInfo);

        var requestFocusFields = !hasCurrentTestNameHeader ? DiagramFocus.ConsumePendingRequestFocus() : null;
        var responseFocusFields = !hasCurrentTestNameHeader ? DiagramFocus.ConsumePendingResponseFocus() : null;

        RequestResponseLogger.Log(new RequestResponseLog(
            currentTestInfo.Name,
            currentTestInfo.Id,
            request.Method,
            requestContentString,
            request.RequestUri!,
            requestHeaders,
            serviceName,
            _callingServiceName!,
            RequestResponseType.Request,
            traceId,
            requestResponseId,
            requestHeaders.Any(x => x.Key == TestTrackingHttpHeaders.Ignore)
        )
        {
            FocusFields = requestFocusFields,
            Timestamp = DateTimeOffset.UtcNow,
            ActivitySpanId = Activity.Current?.SpanId.ToString(),
            ActivityTraceId = Activity.Current?.TraceId.ToString()
        });

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var responseContentString = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseHeaders = response.Headers.SelectMany(x => x.Value.Select(value => (x.Key, (string?)value))).ToArray();

        RequestResponseLogger.Log(new RequestResponseLog(
            currentTestInfo.Name,
            currentTestInfo.Id,
            request.Method,
            responseContentString,
            request.RequestUri!,
            responseHeaders,
            serviceName,
            _callingServiceName!,
            RequestResponseType.Response,
            traceId,
            requestResponseId,
            requestHeaders.Any(x => x.Key == TestTrackingHttpHeaders.Ignore),
            response.StatusCode
            )
        {
            FocusFields = responseFocusFields,
            Timestamp = DateTimeOffset.UtcNow,
            ActivitySpanId = Activity.Current?.SpanId.ToString(),
            ActivityTraceId = Activity.Current?.TraceId.ToString()
        });

        requestActivity?.Stop();
        requestActivity?.Dispose();

        return response;
    }

    private void ForwardHeaders(HttpRequestMessage request)
    {
        if(!_headersToForward.Any())
            return;

        var contextHeaders = _httpContextAccessor?.HttpContext?.Request.Headers;
        if (contextHeaders is null)
            return;

        foreach (var header in _headersToForward)
        {
            if (contextHeaders.TryGetValue(header, out var value))
                request.Headers.Add(header, (IEnumerable<string?>)value);
        }
    }

    private void InjectImplicitActionStartIfNeeded((string Name, string Id) currentTestInfo)
    {
        if (_actionStartInjected || _currentStepTypeFetcher is null)
            return;

        var currentStepType = _currentStepTypeFetcher();
        if (currentStepType is null)
        {
            _lastStepType = null;
            return;
        }

        var isGivenOrAnd = currentStepType.StartsWith("GIVEN", StringComparison.OrdinalIgnoreCase)
                           || currentStepType.StartsWith("AND", StringComparison.OrdinalIgnoreCase)
                           || currentStepType.StartsWith("BUT", StringComparison.OrdinalIgnoreCase);

        if (isGivenOrAnd)
        {
            _wasInGivenSection = true;
        }
        else if (_wasInGivenSection)
        {
            _actionStartInjected = true;
            DefaultTrackingDiagramOverride.StartAction(currentTestInfo.Id);
        }

        _lastStepType = currentStepType;
    }
}