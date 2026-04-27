using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Spanner;

public class SpannerTracker : ITrackingComponent
{
    private readonly SpannerTrackingOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private int _invocationCount;

    public SpannerTracker(SpannerTrackingOptions options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _options = options;
        _httpContextAccessor = httpContextAccessor;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"SpannerTracker ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;
    internal SpannerTrackingOptions Options => _options;

    public (Guid RequestResponseId, Guid TraceId) LogRequest(
        SpannerOperationInfo operation, string? content)
    {
        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return (Guid.Empty, Guid.Empty);

        if (_options.ExcludedOperations.Contains(operation.Operation))
            return (Guid.Empty, Guid.Empty);

        Interlocked.Increment(ref _invocationCount);

        var testInfo = TestInfoResolver.Resolve(_httpContextAccessor, _options.CurrentTestInfoFetcher);
        if (testInfo is null) return (Guid.Empty, Guid.Empty);

        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(
            _options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        var uri = BuildUri(operation, effectiveVerbosity);
        var label = SpannerOperationClassifier.GetDiagramLabel(operation, effectiveVerbosity);
        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        var logContent = effectiveVerbosity == SpannerTrackingVerbosity.Summarised ? null : content;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, logContent, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Request, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event,
            DependencyCategory: "Database")
        {
            Phase = TestPhaseContext.Current
        });

        return (requestResponseId, traceId);
    }

    public void LogResponse(
        SpannerOperationInfo operation,
        Guid requestResponseId, Guid traceId, string? content)
    {
        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction)) return;
        if (_options.ExcludedOperations.Contains(operation.Operation)) return;

        var testInfo = TestInfoResolver.Resolve(_httpContextAccessor, _options.CurrentTestInfoFetcher);
        if (testInfo is null) return;

        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(
            _options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        var uri = BuildUri(operation, effectiveVerbosity);
        var label = SpannerOperationClassifier.GetDiagramLabel(operation, effectiveVerbosity);

        var logContent = effectiveVerbosity == SpannerTrackingVerbosity.Summarised ? null : content;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, logContent, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Response, traceId, requestResponseId, false,
            DependencyCategory: "Database")
        {
            Phase = TestPhaseContext.Current
        });
    }

    private static Uri BuildUri(SpannerOperationInfo op, SpannerTrackingVerbosity effectiveVerbosity)
    {
        var db = op.DatabaseId ?? "unknown";
        var table = op.TableName;

        return effectiveVerbosity switch
        {
            SpannerTrackingVerbosity.Raw when op.DatabaseId is not null =>
                new Uri($"spanner:///{op.DatabaseId}" + (table is not null ? $"/{table}" : "")),
            SpannerTrackingVerbosity.Raw =>
                new Uri($"spanner:///unknown" + (table is not null ? $"/{table}" : "")),
            _ when table is not null =>
                new Uri($"spanner:///{table}"),
            _ =>
                new Uri($"spanner:///{db}")
        };
    }
}
