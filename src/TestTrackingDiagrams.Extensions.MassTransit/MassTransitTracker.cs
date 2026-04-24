using System.Text.Json;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.MassTransit;

public class MassTransitTracker : ITrackingComponent
{
    private readonly MassTransitTrackingOptions _options;
    private int _invocationCount;

    public MassTransitTracker(MassTransitTrackingOptions options)
    {
        _options = options;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"MassTransitTracker ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    public void LogSend(MassTransitOperationInfo op, object? message)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!_options.TrackSend) return;
        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return;
        LogOutgoing(op, message);
    }

    public void LogPublish(MassTransitOperationInfo op, object? message)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!_options.TrackPublish) return;
        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return;
        LogOutgoing(op, message);
    }

    public void LogConsume(MassTransitOperationInfo op, object? message)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!_options.TrackConsume) return;
        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return;
        LogIncoming(op, message);
    }

    public void LogSendFault(MassTransitOperationInfo op, Exception exception)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!_options.LogFaults) return;
        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return;
        LogFault(op, exception, outgoing: true);
    }

    public void LogPublishFault(MassTransitOperationInfo op, Exception exception)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!_options.LogFaults) return;
        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return;
        LogFault(op, exception, outgoing: true);
    }

    public void LogConsumeFault(MassTransitOperationInfo op, Exception exception)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!_options.LogFaults) return;
        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return;
        LogFault(op, exception, outgoing: false);
    }

    private void LogOutgoing(MassTransitOperationInfo op, object? message)
    {
        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null) return;

        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);
        var label = MassTransitOperationClassifier.GetDiagramLabel(op, effectiveVerbosity);
        var uri = MassTransitOperationClassifier.BuildUri(op, effectiveVerbosity);

        var body = _options.LogMessageBody && effectiveVerbosity != MassTransitTrackingVerbosity.Summarised && message is not null
            ? JsonSerializer.Serialize(message, message.GetType())
            : null;

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, body, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Request, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event,
            DependencyCategory: "MessageQueue")
        {
            Phase = TestPhaseContext.Current
        });

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, null, uri, [],
            _options.ServiceName, _options.CallingServiceName,
            RequestResponseType.Response, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event,
            DependencyCategory: "MessageQueue")
        {
            Phase = TestPhaseContext.Current
        });
    }

    private void LogIncoming(MassTransitOperationInfo op, object? message)
    {
        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null) return;

        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);
        var label = MassTransitOperationClassifier.GetDiagramLabel(op, effectiveVerbosity);
        var uri = MassTransitOperationClassifier.BuildUri(op, effectiveVerbosity);

        var body = _options.LogMessageBody && effectiveVerbosity != MassTransitTrackingVerbosity.Summarised && message is not null
            ? JsonSerializer.Serialize(message, message.GetType())
            : null;

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        // Consume is incoming: swap caller/service to reflect message direction
        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, body, uri, [],
            _options.CallingServiceName, _options.ServiceName,
            RequestResponseType.Request, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event,
            DependencyCategory: "MessageQueue")
        {
            Phase = TestPhaseContext.Current
        });

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, null, uri, [],
            _options.CallingServiceName, _options.ServiceName,
            RequestResponseType.Response, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event,
            DependencyCategory: "MessageQueue")
        {
            Phase = TestPhaseContext.Current
        });
    }

    private void LogFault(MassTransitOperationInfo op, Exception exception, bool outgoing)
    {
        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null) return;

        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);
        var label = MassTransitOperationClassifier.GetDiagramLabel(op, effectiveVerbosity);
        var uri = MassTransitOperationClassifier.BuildUri(op, effectiveVerbosity);

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        var svc = outgoing ? _options.ServiceName : _options.CallingServiceName;
        var caller = outgoing ? _options.CallingServiceName : _options.ServiceName;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, exception.Message, uri, [],
            svc, caller,
            RequestResponseType.Request, traceId, requestResponseId, false,
            MetaType: RequestResponseMetaType.Event,
            DependencyCategory: "MessageQueue")
        {
            Phase = TestPhaseContext.Current
        });

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name, testInfo.Value.Id,
            label, null, uri, [],
            svc, caller,
            RequestResponseType.Response, traceId, requestResponseId, false,
            StatusCode: "Fault",
            MetaType: RequestResponseMetaType.Event,
            DependencyCategory: "MessageQueue")
        {
            Phase = TestPhaseContext.Current
        });
    }
}
