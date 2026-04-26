using System.Collections.Concurrent;
using System.Net;
using global::MongoDB.Bson;
using global::MongoDB.Driver.Core.Events;
using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.MongoDB;

public class MongoDbTrackingSubscriber : ITrackingComponent
{
    private readonly MongoDbTrackingOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ConcurrentDictionary<int, PendingOperation> _pending = new();
    private int _invocationCount;

    public MongoDbTrackingSubscriber(MongoDbTrackingOptions options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _options = options;
        _httpContextAccessor = httpContextAccessor;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"MongoDbTrackingSubscriber ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    /// <summary>
    /// Subscribe this tracker to a ClusterBuilder.
    /// Call from MongoClientSettings.ClusterConfigurator.
    /// </summary>
    public void Subscribe(global::MongoDB.Driver.Core.Configuration.ClusterBuilder builder)
    {
        builder.Subscribe<CommandStartedEvent>(OnCommandStarted);
        builder.Subscribe<CommandSucceededEvent>(OnCommandSucceeded);
        builder.Subscribe<CommandFailedEvent>(OnCommandFailed);
    }

    public void OnCommandStarted(CommandStartedEvent e)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return;
        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        if (_options.IgnoredCommands.Contains(e.CommandName)) return;
        if (!_options.TrackGetMore && e.CommandName.Equals("getMore", StringComparison.OrdinalIgnoreCase)) return;

        var testInfo = TestInfoResolver.Resolve(_httpContextAccessor, _options.CurrentTestInfoFetcher);
        if (testInfo is null) return;

        var opInfo = MongoDbOperationClassifier.Classify(
            e.CommandName,
            e.DatabaseNamespace?.DatabaseName,
            e.Command);

        if (effectiveVerbosity == MongoDbTrackingVerbosity.Summarised &&
            opInfo.Operation == MongoDbOperation.Other)
            return;

        var uri = BuildUri(opInfo, effectiveVerbosity);
        var label = MongoDbOperationClassifier.GetDiagramLabel(opInfo, effectiveVerbosity);

        var content = effectiveVerbosity switch
        {
            MongoDbTrackingVerbosity.Summarised => null,
            MongoDbTrackingVerbosity.Raw => e.Command?.ToString(),
            _ => opInfo.FilterText // Detailed: show the filter
        };

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        _pending[e.RequestId] = new PendingOperation(
            testInfo.Value, opInfo, uri, label, traceId, requestResponseId);

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label,
            content, uri,
            [], _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Request, traceId, requestResponseId, false,
            DependencyCategory: "MongoDB")
        {
            Phase = TestPhaseContext.Current
        });
    }

    public void OnCommandSucceeded(CommandSucceededEvent e)
    {
        if (!_pending.TryRemove(e.RequestId, out var pending)) return;

        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        var content = effectiveVerbosity switch
        {
            MongoDbTrackingVerbosity.Summarised => null,
            MongoDbTrackingVerbosity.Raw => e.Reply?.ToString(),
            _ => null // Detailed: no response body
        };

        RequestResponseLogger.Log(new RequestResponseLog(
            pending.TestInfo.Name, pending.TestInfo.Id,
            pending.Label,
            content, pending.Uri,
            [], _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Response, pending.TraceId, pending.RequestResponseId, false,
            HttpStatusCode.OK,
            DependencyCategory: "MongoDB")
        {
            Phase = TestPhaseContext.Current
        });
    }

    public void OnCommandFailed(CommandFailedEvent e)
    {
        if (!_pending.TryRemove(e.RequestId, out var pending)) return;

        RequestResponseLogger.Log(new RequestResponseLog(
            pending.TestInfo.Name, pending.TestInfo.Id,
            pending.Label,
            e.Failure?.Message, pending.Uri,
            [], _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Response, pending.TraceId, pending.RequestResponseId, false,
            HttpStatusCode.InternalServerError,
            DependencyCategory: "MongoDB")
        {
            Phase = TestPhaseContext.Current
        });
    }

    private Uri BuildUri(MongoDbOperationInfo opInfo, MongoDbTrackingVerbosity verbosity)
    {
        var db = opInfo.DatabaseName ?? "unknown";
        var coll = opInfo.CollectionName;

        return verbosity switch
        {
            MongoDbTrackingVerbosity.Summarised =>
                new Uri($"mongodb:///{db}"),
            _ => coll != null
                ? new Uri($"mongodb:///{db}/{coll}")
                : new Uri($"mongodb:///{db}")
        };
    }

    private record PendingOperation(
        (string Name, string Id) TestInfo,
        MongoDbOperationInfo OpInfo,
        Uri Uri,
        string Label,
        Guid TraceId,
        Guid RequestResponseId);
}
