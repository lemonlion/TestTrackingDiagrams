using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.S3;

public class S3TrackingMessageHandler : DelegatingHandler, ITrackingComponent
{
    private readonly S3TrackingMessageHandlerOptions _options;
    private int _invocationCount;

    public S3TrackingMessageHandler(S3TrackingMessageHandlerOptions options, HttpMessageHandler? innerHandler = null)
    {
        _options = options;
        InnerHandler = innerHandler ?? new HttpClientHandler();
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"S3TrackingMessageHandler ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);

        var s3Op = S3OperationClassifier.Classify(request);

        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        if (effectiveVerbosity == S3TrackingVerbosity.Summarised && s3Op.Operation == S3Operation.Other)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var requestResponseId = Guid.NewGuid();
        var traceId = Guid.NewGuid();

        var label = S3OperationClassifier.GetDiagramLabel(s3Op, effectiveVerbosity);

        var requestContent = await GetRequestContent(request, effectiveVerbosity, cancellationToken);
        var requestHeaders = GetFilteredHeaders(request, effectiveVerbosity);

        OneOf<HttpMethod, string> method = effectiveVerbosity == S3TrackingVerbosity.Raw
            ? request.Method
            : label!;

        var requestUri = effectiveVerbosity == S3TrackingVerbosity.Raw
            ? request.RequestUri!
            : BuildCleanUri(s3Op, effectiveVerbosity);

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
            false,
            DependencyCategory: "S3"
        )
        {
            Phase = TestPhaseContext.Current
        });

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var responseContent = await GetResponseContent(response, effectiveVerbosity, cancellationToken);
        var responseHeaders = GetFilteredHeaders(response, effectiveVerbosity);

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
            response.StatusCode,
            DependencyCategory: "S3"
        )
        {
            Phase = TestPhaseContext.Current
        });

        return response;
    }

    private async Task<string?> GetRequestContent(HttpRequestMessage request, S3TrackingVerbosity verbosity, CancellationToken ct)
    {
        if (request.Content is null)
            return null;

        if (verbosity == S3TrackingVerbosity.Summarised)
            return null;

        return await request.Content.ReadAsStringAsync(ct);
    }

    private async Task<string?> GetResponseContent(HttpResponseMessage response, S3TrackingVerbosity verbosity, CancellationToken ct)
    {
        if (verbosity == S3TrackingVerbosity.Summarised)
            return null;

        return await response.Content.ReadAsStringAsync(ct);
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpRequestMessage request, S3TrackingVerbosity verbosity)
    {
        if (verbosity == S3TrackingVerbosity.Summarised)
            return [];

        return request.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpResponseMessage response, S3TrackingVerbosity verbosity)
    {
        if (verbosity == S3TrackingVerbosity.Summarised)
            return [];

        return response.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private static Uri BuildCleanUri(S3OperationInfo op, S3TrackingVerbosity verbosity)
    {
        if (op.BucketName is null)
            return new Uri("s3:///");

        if (verbosity == S3TrackingVerbosity.Summarised)
            return new Uri($"s3://{op.BucketName}/");

        // Detailed: s3://bucket/key
        return op.KeyName is not null
            ? new Uri($"s3://{op.BucketName}/{op.KeyName}")
            : new Uri($"s3://{op.BucketName}/");
    }
}
