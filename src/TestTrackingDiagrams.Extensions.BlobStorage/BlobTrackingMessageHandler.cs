using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.BlobStorage;

public class BlobTrackingMessageHandler : DelegatingHandler
{
    private readonly BlobTrackingMessageHandlerOptions _options;

    public BlobTrackingMessageHandler(BlobTrackingMessageHandlerOptions options, HttpMessageHandler? innerHandler = null)
    {
        _options = options;
        InnerHandler = innerHandler ?? new HttpClientHandler();
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var blobOp = BlobOperationClassifier.Classify(request);

        // Skip internal/metadata operations when in Summarised mode
        if (_options.Verbosity == BlobTrackingVerbosity.Summarised && blobOp.Operation == BlobOperation.Other)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var requestResponseId = Guid.NewGuid();
        var traceId = Guid.NewGuid();

        var label = BlobOperationClassifier.GetDiagramLabel(blobOp, _options.Verbosity);

        var requestContent = await GetRequestContent(request, cancellationToken);
        var requestHeaders = GetFilteredHeaders(request);

        OneOf<HttpMethod, string> method = _options.Verbosity == BlobTrackingVerbosity.Raw
            ? request.Method
            : label;

        var requestUri = _options.Verbosity == BlobTrackingVerbosity.Raw
            ? request.RequestUri!
            : BuildCleanUri(request.RequestUri!, blobOp, _options.Verbosity);

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

        if (_options.Verbosity == BlobTrackingVerbosity.Summarised)
            return null;

        return await request.Content.ReadAsStringAsync(ct);
    }

    private async Task<string?> GetResponseContent(HttpResponseMessage response, CancellationToken ct)
    {
        if (_options.Verbosity == BlobTrackingVerbosity.Summarised)
            return null;

        return await response.Content.ReadAsStringAsync(ct);
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpRequestMessage request)
    {
        if (_options.Verbosity == BlobTrackingVerbosity.Summarised)
            return [];

        return request.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpResponseMessage response)
    {
        if (_options.Verbosity == BlobTrackingVerbosity.Summarised)
            return [];

        return response.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private static Uri BuildCleanUri(Uri originalUri, BlobOperationInfo op, BlobTrackingVerbosity verbosity)
    {
        if (op.ContainerName is null)
            return originalUri;

        if (verbosity == BlobTrackingVerbosity.Summarised)
        {
            var builder = new UriBuilder(originalUri)
            {
                Path = op.BlobName is not null
                    ? $"/{op.ContainerName}/{op.BlobName}"
                    : $"/{op.ContainerName}",
                Query = ""
            };
            return builder.Uri;
        }

        // Detailed: container/blob, strip query params
        var cleanPath = op.BlobName is not null
            ? $"/{op.ContainerName}/{op.BlobName}"
            : $"/{op.ContainerName}";

        var uriBuilder = new UriBuilder(originalUri) { Path = cleanPath, Query = "" };
        return uriBuilder.Uri;
    }
}
