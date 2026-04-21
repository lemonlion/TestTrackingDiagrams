using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.BigQuery;

public class BigQueryTrackingMessageHandler : DelegatingHandler, ITrackingComponent
{
    private readonly BigQueryTrackingMessageHandlerOptions _options;
    private int _invocationCount;

    public BigQueryTrackingMessageHandler(BigQueryTrackingMessageHandlerOptions options, HttpMessageHandler? innerHandler = null)
    {
        _options = options;
        InnerHandler = innerHandler ?? new HttpClientHandler();
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"BigQueryTrackingMessageHandler ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);

        var bqOp = BigQueryOperationClassifier.Classify(request);

        // Skip unrecognised operations when in Summarised mode
        if (_options.Verbosity == BigQueryTrackingVerbosity.Summarised && bqOp.Operation == BigQueryOperation.Other)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var requestResponseId = Guid.NewGuid();
        var traceId = Guid.NewGuid();

        var label = BigQueryOperationClassifier.GetDiagramLabel(bqOp, _options.Verbosity);

        var requestContent = await GetRequestContent(request, cancellationToken);
        var requestHeaders = GetFilteredHeaders(request);

        OneOf<HttpMethod, string> method = _options.Verbosity == BigQueryTrackingVerbosity.Raw
            ? request.Method
            : label!;

        var requestUri = _options.Verbosity == BigQueryTrackingVerbosity.Raw
            ? request.RequestUri!
            : BuildCleanUri(request.RequestUri!, bqOp, _options.Verbosity);

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name,
            testInfo.Value.Id,
            method,
            requestContent,
            requestUri,
            requestHeaders,
            _options.ServiceName,
            _options.CallingServiceName,
            RequestResponseType.Request,
            traceId,
            requestResponseId,
            false
        ));

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var responseContent = await GetResponseContent(response, cancellationToken);
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

    private async Task<string?> GetRequestContent(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Content is null)
            return null;

        if (_options.Verbosity == BigQueryTrackingVerbosity.Summarised)
            return null;

        return await request.Content.ReadAsStringAsync(ct);
    }

    private async Task<string?> GetResponseContent(HttpResponseMessage response, CancellationToken ct)
    {
        if (_options.Verbosity == BigQueryTrackingVerbosity.Summarised)
            return null;

        return await response.Content.ReadAsStringAsync(ct);
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpRequestMessage request)
    {
        if (_options.Verbosity == BigQueryTrackingVerbosity.Summarised)
            return [];

        return request.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpResponseMessage response)
    {
        if (_options.Verbosity == BigQueryTrackingVerbosity.Summarised)
            return [];

        return response.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private static Uri BuildCleanUri(Uri originalUri, BigQueryOperationInfo op, BigQueryTrackingVerbosity verbosity)
    {
        if (op.ResourceType is null)
            return originalUri;

        if (verbosity == BigQueryTrackingVerbosity.Summarised)
        {
            var path = op.ResourceName is not null
                ? $"/{op.ResourceType}/{op.ResourceName}"
                : $"/{op.ResourceType}";
            return new UriBuilder(originalUri) { Path = path, Query = "" }.Uri;
        }

        // Detailed: include dataset context if available
        var parts = new List<string>();
        if (op.DatasetId is not null)
            parts.Add(op.DatasetId);
        if (op.ResourceType != "dataset" || op.ResourceName is null)
            parts.Add(op.ResourceType);
        if (op.ResourceName is not null)
            parts.Add(op.ResourceName);

        var cleanPath = "/" + string.Join("/", parts);
        return new UriBuilder(originalUri) { Path = cleanPath, Query = "" }.Uri;
    }
}
