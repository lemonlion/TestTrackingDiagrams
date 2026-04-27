using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using TestTrackingDiagrams.Constants;

namespace TestTrackingDiagrams.Tracking;

public class TestTrackingMessageHandler : DelegatingHandler, ITrackingComponent
{
    private readonly string? _fixedServiceName;
    private readonly Func<int, string> _getServiceNameFromPortTranslator;
    private readonly Dictionary<string, string> _clientNamesToServiceNames;
    private readonly string? _clientName;
    private readonly string? _callingServiceName;
    private readonly Func<(string Name, string Id)>? _currentTestInfoFetcher;
    private readonly Func<string?>? _currentStepTypeFetcher;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IEnumerable<string> _headersToForward;
    private readonly string[]? _internalFlowActivitySources;
    private readonly bool _trackDuringSetup;
    private readonly bool _trackDuringAction;
    private string? _lastStepType;
    private bool _wasInGivenSection;
    private bool _actionStartInjected;
    private int _invocationCount;
    private bool _listenerStarted;

    public TestTrackingMessageHandler(TestTrackingMessageHandlerOptions options, IHttpContextAccessor? httpContextAccessor = null, string? clientName = null)
    {
        _fixedServiceName = options.FixedNameForReceivingService;
        _getServiceNameFromPortTranslator = GetPortTranslator(options.PortsToServiceNames);
        _clientNamesToServiceNames = options.ClientNamesToServiceNames;
        _clientName = clientName;
        _currentTestInfoFetcher = options.CurrentTestInfoFetcher;
        _currentStepTypeFetcher = options.CurrentStepTypeFetcher;
        _callingServiceName = options.CallingServiceName;
        _httpContextAccessor = httpContextAccessor ?? options.HttpContextAccessor;
        _headersToForward = options.HeadersToForward;
        _internalFlowActivitySources = options.InternalFlowActivitySources;
        _trackDuringSetup = options.TrackDuringSetup;
        _trackDuringAction = options.TrackDuringAction;
        InnerHandler ??= new HttpClientHandler();

        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"TestTrackingMessageHandler ({_callingServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    private static Func<int, string> GetPortTranslator(Dictionary<int, string> serviceNamesForEachPort)
    {
        return port => serviceNamesForEachPort.TryGetValue(port, out var serviceName) ? serviceName : $"localhost:{port}";
    }

    private string ResolveServiceName(int port)
    {
        // 1. FixedNameForReceivingService (highest priority)
        if (_fixedServiceName is not null)
            return _fixedServiceName;

        // 2. Client name mapping (set via constructor clientName parameter)
        if (_clientName is not null && _clientNamesToServiceNames.TryGetValue(_clientName, out var mapped))
            return mapped;

        // 3. Port-based mapping or fallback to localhost:port
        return _getServiceNameFromPortTranslator(port);
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);

        // Deferred start — registering an ActivityListener during DI resolution
        // can alter ActivitySource.HasListeners() state before the host and
        // Application Insights' DependencyTrackingTelemetryModule have fully
        // initialised, breaking HTTP dependency telemetry. Starting on first
        // use guarantees all other services are ready.
        if (!_listenerStarted)
        {
            InternalFlow.InternalFlowActivityListener.EnsureStarted(_internalFlowActivitySources);
            _listenerStarted = true;
        }

        ForwardHeaders(request);

        var requestResponseId = Guid.NewGuid();

        // Ensure trace context propagation for in-process (TestServer) scenarios
        // where no framework DiagnosticsHandler exists in the pipeline.
        // When Activity.Current IS present, a framework handler (e.g.
        // DiagnosticsHandler inside SocketsHttpHandler) will create a proper
        // child Activity and inject traceparent itself — pre-empting it here
        // would inject the PARENT's span ID, breaking AI SDK dependency
        // correlation. We therefore only inject when no ambient Activity exists.
        string? activityTraceId;
        string? activitySpanId;
        if (Activity.Current != null)
        {
            activityTraceId = Activity.Current.TraceId.ToString();
            activitySpanId = Activity.Current.SpanId.ToString();
        }
        else
        {
            activityTraceId = ActivityTraceId.CreateRandom().ToString();
            activitySpanId = ActivitySpanId.CreateRandom().ToString();
            if (!request.Headers.Contains("traceparent"))
            {
                request.Headers.TryAddWithoutValidation("traceparent",
                    $"00-{activityTraceId}-{activitySpanId}-00");
            }
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

        // Resolve test info once — if the fetcher throws, skip all tracking and just forward the request.
        (string Name, string Id) currentTestInfo;
        try
        {
            if (currentTestInfoFetcher is null)
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            currentTestInfo = currentTestInfoFetcher();
        }
        catch
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var traceId = hasTraceIdHeader ? Guid.Parse(traceIdHeaders.First()!) : Guid.NewGuid();

        if (!hasTraceIdHeader)
            request.Headers.Add(TestTrackingHttpHeaders.TraceIdHeader, new[] { traceId.ToString() });

        if (!hasCurrentTestNameHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CurrentTestNameHeader, new[] { currentTestInfo.Name });

        if (!hasCurrentTestIdHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CurrentTestIdHeader, new[] { currentTestInfo.Id.ToString() });

        if (!hasCallerNameHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CallerNameHeader, new[] { _callingServiceName! });

        var serviceName = ResolveServiceName(request.RequestUri!.Port);

        if (!hasCurrentTestNameHeader)
            InjectImplicitActionStartIfNeeded(currentTestInfo);

        var requestFocusFields = !hasCurrentTestNameHeader ? DiagramFocus.ConsumePendingRequestFocus() : null;
        var responseFocusFields = !hasCurrentTestNameHeader ? DiagramFocus.ConsumePendingResponseFocus() : null;

        var trackingIgnore = requestHeaders.Any(x => x.Key == TestTrackingHttpHeaders.Ignore)
                             || !PhaseConfiguration.ShouldTrack(_trackDuringSetup, _trackDuringAction);
        var currentPhase = TestPhaseContext.Current;

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
            trackingIgnore
        )
        {
            FocusFields = requestFocusFields,
            Timestamp = DateTimeOffset.UtcNow,
            ActivitySpanId = activitySpanId,
            ActivityTraceId = activityTraceId,
            Phase = currentPhase
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
            trackingIgnore,
            response.StatusCode
            )
        {
            FocusFields = responseFocusFields,
            Timestamp = DateTimeOffset.UtcNow,
            ActivitySpanId = activitySpanId,
            ActivityTraceId = activityTraceId,
            Phase = currentPhase
        });

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