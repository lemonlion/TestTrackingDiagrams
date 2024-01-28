using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using TestTrackingDiagrams.Constants;

namespace TestTrackingDiagrams.Tracking;

public class TestTrackingMessageHandler : DelegatingHandler
{
    private readonly Func<int, string> _getServiceNameFromPortTranslator;
    private readonly string? _callingServiceName;
    private readonly Func<(string Name, Guid Id)> _currentTestInfoFetcher;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public TestTrackingMessageHandler(Func<int, string> getServiceNameFromPortTranslator, Func<(string Name, Guid Id)> currentTestInfoFetcher, string? callingServiceName = "Caller", IHttpContextAccessor? httpContextAccessor = null)
    {
        _getServiceNameFromPortTranslator = getServiceNameFromPortTranslator;
        _currentTestInfoFetcher = currentTestInfoFetcher;
        _callingServiceName = callingServiceName;
        _httpContextAccessor = httpContextAccessor;
        InnerHandler ??= new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestResponseId = Guid.NewGuid();

        var requestContentString = request.Content is null ? null : await request.Content!.ReadAsStringAsync(cancellationToken);
        var requestHeaders = request.Headers.Select(x => (x.Key, x.Value.First())).ToArray();

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

        var currentTestInfoFetcher = hasCurrentTestNameHeader ? () => (currentTestNameHeaders.First(), Guid.Parse(currentTestIdHeaders.First())) : _currentTestInfoFetcher;

        var traceId = hasTraceIdHeader ? Guid.Parse(traceIdHeaders.First()) : Guid.NewGuid();

        if (!hasTraceIdHeader)
            request.Headers.Add(TestTrackingHttpHeaders.TraceIdHeader, new[] { traceId.ToString() });

        if (!hasCurrentTestNameHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CurrentTestNameHeader, new[] { currentTestInfoFetcher().Name });

        if (!hasCurrentTestIdHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CurrentTestIdHeader, new[] { currentTestInfoFetcher().Id.ToString() });

        if (!hasCallerNameHeader)
            request.Headers.Add(TestTrackingHttpHeaders.CallerNameHeader, new[] { "Caller" });

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
        var responseHeaders = response.Headers.Select(x => (x.Key, x.Value.First())).ToArray();
        
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
}