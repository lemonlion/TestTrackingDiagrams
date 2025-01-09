using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using TestTrackingDiagrams.Constants;

namespace TestTrackingDiagrams.Tracking;

public class TestTrackingMessageHandler : DelegatingHandler
{
    private readonly Func<int, string> _getServiceNameFromPortTranslator;
    private readonly string? _callingServiceName;
    private readonly Func<(string Name, string Id)>? _currentTestInfoFetcher;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IEnumerable<string> _headersToForward;

    public TestTrackingMessageHandler(TestTrackingMessageHandlerOptions options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _getServiceNameFromPortTranslator = options.FixedNameForReceivingService is not null ? _ => options.FixedNameForReceivingService : GetPortTranslator(options.PortsToServiceNames);
        _currentTestInfoFetcher = options.CurrentTestInfoFetcher;
        _callingServiceName = options.CallingServiceName;
        _httpContextAccessor = httpContextAccessor;
        _headersToForward = options.HeadersToForward;
        InnerHandler ??= new HttpClientHandler();
    }

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
        ForwardHeaders(request);

        var requestResponseId = Guid.NewGuid();

        var requestContentString = request.Content is null ? null : await request.Content!.ReadAsStringAsync(cancellationToken);
        var requestHeaders = request.Headers.SelectMany(x => x.Value.Select(value => (x.Key, value))).ToArray();

        StringValues currentTestNameHeaders = new();
        var hasCurrentTestNameHeader = false;

        StringValues currentTestIdHeaders = new();
        var hasCurrentTestIdHeader = false;

        StringValues callerNameHeaders = new();
        var hasCallerNameHeader = false;

        StringValues traceIdHeaders = new();
        var hasTraceIdHeader = false;

        if (_httpContextAccessor is not null)
        {
            hasTraceIdHeader = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.TraceIdHeader, out traceIdHeaders);
            hasCurrentTestNameHeader = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestNameHeader, out currentTestNameHeaders);
            hasCurrentTestIdHeader = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestIdHeader, out currentTestIdHeaders);
            hasCallerNameHeader = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CallerNameHeader, out callerNameHeaders);
        }

        var currentTestInfoFetcher = hasCurrentTestNameHeader ? () => (currentTestNameHeaders.First(), currentTestIdHeaders.First()) : _currentTestInfoFetcher;

        var traceId = hasTraceIdHeader ? Guid.Parse(traceIdHeaders.First()) : Guid.NewGuid();

        if (!hasTraceIdHeader)
            request.Headers.Add(TestTrackingHttpHeaders.TraceIdHeader, new[] { traceId.ToString() });

        if (!hasCurrentTestNameHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CurrentTestNameHeader, new[] { currentTestInfoFetcher().Name });

        if (!hasCurrentTestIdHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CurrentTestIdHeader, new[] { currentTestInfoFetcher().Id.ToString() });

        if (!hasCallerNameHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CallerNameHeader, new[] { _callingServiceName });

        var currentTestInfo = currentTestInfoFetcher();

        var serviceName = _getServiceNameFromPortTranslator(request.RequestUri.Port);

        RequestResponseLogger.Log(new RequestResponseLog(
            currentTestInfo.Name,
            currentTestInfo.Id,
            request.Method,
            requestContentString,
            request.RequestUri!,
            requestHeaders,
            serviceName,
            _callingServiceName,
            RequestResponseType.Request,
            traceId,
            requestResponseId,
            requestHeaders.Any(x => x.Key == TestTrackingHttpHeaders.Ignore)
        ));

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var responseContentString = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseHeaders = response.Headers.SelectMany(x => x.Value.Select(value => (x.Key, value))).ToArray();

        RequestResponseLogger.Log(new RequestResponseLog(
            currentTestInfo.Name,
            currentTestInfo.Id,
            request.Method,
            responseContentString,
            request.RequestUri!,
            responseHeaders,
            serviceName,
            _callingServiceName,
            RequestResponseType.Response,
            traceId,
            requestResponseId,
            requestHeaders.Any(x => x.Key == TestTrackingHttpHeaders.Ignore),
            response.StatusCode
            ));

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
}