using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.CloudStorage;

public class CloudStorageTrackingMessageHandler : DelegatingHandler, ITrackingComponent
{
    private readonly CloudStorageTrackingMessageHandlerOptions _options;
    private int _invocationCount;

    public CloudStorageTrackingMessageHandler(CloudStorageTrackingMessageHandlerOptions options, HttpMessageHandler? innerHandler = null)
    {
        _options = options;
        InnerHandler = innerHandler ?? new HttpClientHandler();
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"CloudStorageTrackingMessageHandler ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);

        var gcsOp = CloudStorageOperationClassifier.Classify(request);

        if (_options.Verbosity == CloudStorageTrackingVerbosity.Summarised && gcsOp.Operation == CloudStorageOperation.Other)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var requestResponseId = Guid.NewGuid();
        var traceId = Guid.NewGuid();

        var label = CloudStorageOperationClassifier.GetDiagramLabel(gcsOp, _options.Verbosity);

        var requestContent = await GetRequestContent(request, cancellationToken);
        var requestHeaders = GetFilteredHeaders(request);

        OneOf<HttpMethod, string> method = _options.Verbosity == CloudStorageTrackingVerbosity.Raw
            ? request.Method
            : label;

        var requestUri = _options.Verbosity == CloudStorageTrackingVerbosity.Raw
            ? request.RequestUri!
            : BuildCleanUri(gcsOp);

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
        if (request.Content is null) return null;
        if (_options.Verbosity == CloudStorageTrackingVerbosity.Summarised) return null;
        return await request.Content.ReadAsStringAsync(ct);
    }

    private async Task<string?> GetResponseContent(HttpResponseMessage response, CancellationToken ct)
    {
        if (_options.Verbosity == CloudStorageTrackingVerbosity.Summarised) return null;
        return await response.Content.ReadAsStringAsync(ct);
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpRequestMessage request)
    {
        if (_options.Verbosity == CloudStorageTrackingVerbosity.Summarised) return [];
        return request.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpResponseMessage response)
    {
        if (_options.Verbosity == CloudStorageTrackingVerbosity.Summarised) return [];
        return response.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private static Uri BuildCleanUri(CloudStorageOperationInfo op)
    {
        if (op.BucketName is null)
            return new Uri("gcs:///");

        if (op.ObjectName is not null)
            return new Uri($"gcs:///{op.BucketName}/{op.ObjectName}");

        return new Uri($"gcs:///{op.BucketName}");
    }
}
