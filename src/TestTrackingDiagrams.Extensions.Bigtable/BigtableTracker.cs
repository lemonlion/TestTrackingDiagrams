using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Bigtable;

public class BigtableTracker : ITrackingComponent
{
    private readonly BigtableTrackingOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private int _invocationCount;

    public BigtableTracker(BigtableTrackingOptions options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _options = options;
        _httpContextAccessor = httpContextAccessor;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"BigtableTracker ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    public (Guid RequestResponseId, Guid TraceId) LogRequest(
        BigtableOperationInfo operation, string? content)
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
        var label = BigtableOperationClassifier.GetDiagramLabel(operation, effectiveVerbosity);
        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        var logContent = effectiveVerbosity == BigtableTrackingVerbosity.Summarised ? null : content;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, logContent, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Request, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event,
            DependencyCategory: "Bigtable")
        {
            Phase = TestPhaseContext.Current
        });

        return (requestResponseId, traceId);
    }

    public void LogResponse(
        BigtableOperationInfo operation,
        Guid requestResponseId, Guid traceId, string? content)
    {
        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction)) return;
        if (_options.ExcludedOperations.Contains(operation.Operation)) return;

        var testInfo = TestInfoResolver.Resolve(_httpContextAccessor, _options.CurrentTestInfoFetcher);
        if (testInfo is null) return;

        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(
            _options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        var uri = BuildUri(operation, effectiveVerbosity);
        var label = BigtableOperationClassifier.GetDiagramLabel(operation, effectiveVerbosity);

        var logContent = effectiveVerbosity == BigtableTrackingVerbosity.Summarised ? null : content;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, logContent, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Response, traceId, requestResponseId, false,
            DependencyCategory: "Bigtable")
        {
            Phase = TestPhaseContext.Current
        });
    }

    private static Uri BuildUri(BigtableOperationInfo op, BigtableTrackingVerbosity effectiveVerbosity)
    {
        var table = ShortTableName(op.TableName);

        return effectiveVerbosity switch
        {
            BigtableTrackingVerbosity.Raw when op.TableName is not null =>
                new Uri($"bigtable:///{op.TableName}"),
            BigtableTrackingVerbosity.Raw =>
                new Uri("bigtable:///unknown"),
            _ when table is not null =>
                new Uri($"bigtable:///{table}"),
            _ =>
                new Uri("bigtable:///unknown")
        };
    }

    private static string? ShortTableName(string? fullName)
    {
        if (fullName is null) return null;
        var lastSlash = fullName.LastIndexOf('/');
        return lastSlash >= 0 ? fullName[(lastSlash + 1)..] : fullName;
    }
}
