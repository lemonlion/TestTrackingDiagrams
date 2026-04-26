using System.Text;
using Elastic.Transport;
using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Elasticsearch;

public class ElasticsearchTrackingCallbackHandler : ITrackingComponent
{
    private readonly ElasticsearchTrackingOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private int _invocationCount;

    public ElasticsearchTrackingCallbackHandler(ElasticsearchTrackingOptions options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _options = options;
        _httpContextAccessor = httpContextAccessor;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"ElasticsearchTrackingCallbackHandler ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    public void HandleApiCallDetails(ApiCallDetails details)
    {
        LogOperation(
            details.HttpMethod.ToString().ToUpperInvariant(),
            details.Uri,
            details.HttpStatusCode,
            details.RequestBodyInBytes,
            details.ResponseBodyInBytes);
    }

    internal void LogOperation(
        string httpMethod,
        Uri requestUri,
        int? statusCode,
        byte[]? requestBodyBytes,
        byte[]? responseBodyBytes)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return;
        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        var testInfo = TestInfoResolver.Resolve(_httpContextAccessor, _options.CurrentTestInfoFetcher);
        if (testInfo is null) return;

        var method = new System.Net.Http.HttpMethod(httpMethod);
        var op = ElasticsearchOperationClassifier.Classify(method, requestUri);

        if (_options.ExcludedOperations.Contains(op.Operation)) return;

        var label = ElasticsearchOperationClassifier.GetDiagramLabel(op, effectiveVerbosity);
        var uri = ElasticsearchOperationClassifier.BuildUri(op, effectiveVerbosity, requestUri);

        string? requestBody = null;
        if (effectiveVerbosity != ElasticsearchTrackingVerbosity.Summarised
            && requestBodyBytes is not null)
        {
            requestBody = Encoding.UTF8.GetString(requestBodyBytes);
        }

        string? responseBody = null;
        if (effectiveVerbosity == ElasticsearchTrackingVerbosity.Raw
            && responseBodyBytes is not null)
        {
            responseBody = Encoding.UTF8.GetString(responseBodyBytes);
        }

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label,
            requestBody,
            uri,
            [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Request, traceId, requestResponseId, false,
            StatusCode: statusCode.HasValue
                ? (System.Net.HttpStatusCode)statusCode.Value
                : null,
            DependencyCategory: "Elasticsearch")
        {
            Phase = TestPhaseContext.Current
        });

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label,
            responseBody,
            uri,
            [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Response, traceId, requestResponseId, false,
            StatusCode: statusCode.HasValue
                ? (System.Net.HttpStatusCode)statusCode.Value
                : null,
            DependencyCategory: "Elasticsearch")
        {
            Phase = TestPhaseContext.Current
        });
    }
}
