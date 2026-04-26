using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.CosmosDB;

public class CosmosTrackingMessageHandler : DelegatingHandler, ITrackingComponent
{
    private readonly CosmosTrackingMessageHandlerOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private int _invocationCount;

    public CosmosTrackingMessageHandler(CosmosTrackingMessageHandlerOptions options, HttpMessageHandler? innerHandler = null, IHttpContextAccessor? httpContextAccessor = null)
    {
        _options = options;
        _httpContextAccessor = httpContextAccessor;
        InnerHandler = innerHandler ?? new HttpClientHandler();
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"CosmosTrackingMessageHandler ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        var cosmosOp = CosmosOperationClassifier.Classify(request);

        // Skip internal/metadata operations when in Summarised mode
        if (effectiveVerbosity == CosmosTrackingVerbosity.Summarised && cosmosOp.Operation == CosmosOperation.Other)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var testInfo = TestInfoResolver.Resolve(_httpContextAccessor, _options.CurrentTestInfoFetcher);
        if (testInfo is null)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var requestResponseId = Guid.NewGuid();
        var traceId = Guid.NewGuid();

        var label = CosmosOperationClassifier.GetDiagramLabel(cosmosOp, effectiveVerbosity);

        var requestContent = await GetRequestContent(request, cosmosOp, effectiveVerbosity, cancellationToken);
        var requestHeaders = GetFilteredHeaders(request, effectiveVerbosity);

        // Use the label as the "method" for Detailed/Summarised, or the real HTTP method for Raw
        OneOf<HttpMethod, string> method = effectiveVerbosity == CosmosTrackingVerbosity.Raw
            ? request.Method
            : label;

        var requestUri = effectiveVerbosity == CosmosTrackingVerbosity.Raw
            ? request.RequestUri!
            : BuildCleanUri(request.RequestUri!, cosmosOp, effectiveVerbosity);

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
            DependencyCategory: "CosmosDB"
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
            DependencyCategory: "CosmosDB"
        )
        {
            Phase = TestPhaseContext.Current
        });

        return response;
    }

    private async Task<string?> GetRequestContent(HttpRequestMessage request, CosmosOperationInfo op, CosmosTrackingVerbosity verbosity, CancellationToken ct)
    {
        if (request.Content is null)
            return null;

        if (verbosity == CosmosTrackingVerbosity.Summarised)
            return op.QueryText;

        var content = await request.Content.ReadAsStringAsync(ct);

        if (verbosity == CosmosTrackingVerbosity.Detailed && op.Operation == CosmosOperation.Query)
            return op.QueryText;

        return content;
    }

    private async Task<string?> GetResponseContent(HttpResponseMessage response, CosmosTrackingVerbosity verbosity, CancellationToken ct)
    {
        if (verbosity == CosmosTrackingVerbosity.Summarised)
            return null;

        return await response.Content.ReadAsStringAsync(ct);
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpRequestMessage request, CosmosTrackingVerbosity verbosity)
    {
        if (verbosity == CosmosTrackingVerbosity.Summarised)
            return [];

        return request.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private (string Key, string? Value)[] GetFilteredHeaders(HttpResponseMessage response, CosmosTrackingVerbosity verbosity)
    {
        if (verbosity == CosmosTrackingVerbosity.Summarised)
            return [];

        return response.Headers
            .Where(h => !_options.ExcludedHeaders.Contains(h.Key))
            .SelectMany(h => h.Value.Select(v => (h.Key, (string?)v)))
            .ToArray();
    }

    private static Uri BuildCleanUri(Uri originalUri, CosmosOperationInfo op, CosmosTrackingVerbosity verbosity)
    {
        if (op.CollectionName is null)
            return originalUri;

        if (verbosity == CosmosTrackingVerbosity.Summarised)
        {
            var builder = new UriBuilder(originalUri) { Path = $"/{op.CollectionName}" };
            return builder.Uri;
        }

        // Detailed: /colls/{coll} with optional resource path, no db prefix
        var parts = new List<string> { $"colls/{op.CollectionName}" };

        if (op.DocumentId is not null)
        {
            var resourceType = op.Operation == CosmosOperation.ExecStoredProc ? "sprocs" : "docs";
            parts.Add($"{resourceType}/{op.DocumentId}");
        }

        var cleanPath = "/" + string.Join("/", parts);
        var uriBuilder = new UriBuilder(originalUri) { Path = cleanPath };
        return uriBuilder.Uri;
    }
}
