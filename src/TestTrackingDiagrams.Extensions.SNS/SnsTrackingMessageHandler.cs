using System.Text;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.SNS;

public class SnsTrackingMessageHandler : DelegatingHandler, ITrackingComponent
{
    private readonly SnsTrackingMessageHandlerOptions _options;
    private int _invocationCount;

    public SnsTrackingMessageHandler(SnsTrackingMessageHandlerOptions options, HttpMessageHandler? innerHandler = null)
    {
        _options = options;
        InnerHandler = innerHandler ?? new HttpClientHandler();
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"SnsTrackingMessageHandler ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);

        string? requestBody = null;
        if (request.Content is not null)
            requestBody = await request.Content.ReadAsStringAsync(cancellationToken);

        var snsOp = SnsOperationClassifier.Classify(request, requestBody);

        if (_options.Verbosity == SnsTrackingVerbosity.Summarised && snsOp.Operation == SnsOperation.Other)
        {
            ReconstructContent(request, requestBody);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
        {
            ReconstructContent(request, requestBody);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var requestResponseId = Guid.NewGuid();
        var traceId = Guid.NewGuid();

        var label = SnsOperationClassifier.GetDiagramLabel(snsOp, _options.Verbosity);

        var logRequestContent = _options.Verbosity == SnsTrackingVerbosity.Summarised
            ? null
            : requestBody;

        var requestHeaders = GetFilteredHeaders(request);

        OneOf<HttpMethod, string> method = _options.Verbosity == SnsTrackingVerbosity.Raw
            ? request.Method
            : label!;

        var requestUri = _options.Verbosity == SnsTrackingVerbosity.Raw
            ? request.RequestUri!
            : BuildCleanUri(snsOp);

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name,
            testInfo.Value.Id,
            method,
            logRequestContent,
            requestUri,
            requestHeaders,
            _options.ServiceName,
            _options.CallingServiceName,
            RequestResponseType.Request,
            traceId,
            requestResponseId,
            false
        ));

        ReconstructContent(request, requestBody);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var responseContent = _options.Verbosity == SnsTrackingVerbosity.Summarised
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        var responseHeaders = GetFilteredHeaders(response);

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name,
            testInfo.Value.Id,
            method,
            responseContent,
            requestUri,
            responseHeaders,
            _options.ServiceName,
            _options.CallingServiceName,
            RequestResponseType.Response,
            traceId,
            requestResponseId,
            false,
            response.StatusCode
        ));

        return response;
    }

    private static void ReconstructContent(HttpRequestMessage request, string? body)
    {
        if (body is null) return;
        var mediaType = request.Content?.Headers.ContentType?.MediaType ?? "application/x-amz-json-1.0";
        request.Content = new StringContent(body, Encoding.UTF8, mediaType);
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpRequestMessage request)
    {
        if (_options.Verbosity == SnsTrackingVerbosity.Summarised)
            return [];

        return request.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpResponseMessage response)
    {
        if (_options.Verbosity == SnsTrackingVerbosity.Summarised)
            return [];

        return response.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private static Uri BuildCleanUri(SnsOperationInfo op)
    {
        if (op.TopicName is null)
            return new Uri("sns:///");

        return new Uri($"sns:///{op.TopicName}");
    }
}
