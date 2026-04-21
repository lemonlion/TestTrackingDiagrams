using System.Text;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.DynamoDB;

public class DynamoDbTrackingMessageHandler : DelegatingHandler, ITrackingComponent
{
    private readonly DynamoDbTrackingMessageHandlerOptions _options;
    private int _invocationCount;

    public DynamoDbTrackingMessageHandler(DynamoDbTrackingMessageHandlerOptions options, HttpMessageHandler? innerHandler = null)
    {
        _options = options;
        InnerHandler = innerHandler ?? new HttpClientHandler();
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"DynamoDbTrackingMessageHandler ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);

        // Read request body first — needed for classification (table name is in body)
        string? requestBody = null;
        if (request.Content is not null)
            requestBody = await request.Content.ReadAsStringAsync(cancellationToken);

        var dynamoOp = DynamoDbOperationClassifier.Classify(request, requestBody);

        if (_options.Verbosity == DynamoDbTrackingVerbosity.Summarised && dynamoOp.Operation == DynamoDbOperation.Other)
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

        var label = DynamoDbOperationClassifier.GetDiagramLabel(dynamoOp, _options.Verbosity);

        var logRequestContent = _options.Verbosity == DynamoDbTrackingVerbosity.Summarised
            ? null
            : requestBody;

        var requestHeaders = GetFilteredHeaders(request);

        OneOf<HttpMethod, string> method = _options.Verbosity == DynamoDbTrackingVerbosity.Raw
            ? request.Method
            : label!;

        var requestUri = _options.Verbosity == DynamoDbTrackingVerbosity.Raw
            ? request.RequestUri!
            : BuildCleanUri(dynamoOp, _options.Verbosity);

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

        // Reconstruct content before forwarding since ReadAsStringAsync consumed the stream
        ReconstructContent(request, requestBody);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var responseContent = _options.Verbosity == DynamoDbTrackingVerbosity.Summarised
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
        if (_options.Verbosity == DynamoDbTrackingVerbosity.Summarised)
            return [];

        return request.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpResponseMessage response)
    {
        if (_options.Verbosity == DynamoDbTrackingVerbosity.Summarised)
            return [];

        return response.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private static Uri BuildCleanUri(DynamoDbOperationInfo op, DynamoDbTrackingVerbosity verbosity)
    {
        if (op.TableName is null)
            return new Uri("dynamodb:///");

        return new Uri($"dynamodb:///{op.TableName}");
    }
}
