using System.Net;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Grpc;

public class GrpcTrackingInterceptor : Interceptor, ITrackingComponent
{
    private readonly GrpcTrackingOptions _options;
    private int _invocationCount;

    public GrpcTrackingInterceptor(GrpcTrackingOptions options)
    {
        _options = options;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"GrpcTrackingInterceptor ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return continuation(request, context);
        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return continuation(request, context);

        var opInfo = Classify(context);
        var label = GrpcOperationClassifier.GetDiagramLabel(opInfo, effectiveVerbosity);
        var serviceName = ResolveServiceName(opInfo);
        var uri = BuildUri(opInfo, effectiveVerbosity);
        var requestContent = SerializeMessage(request, effectiveVerbosity);
        var headers = GetCallHeaders(context);

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        LogRequest(testInfo.Value, label, requestContent, uri, headers, serviceName, traceId, requestResponseId);

        var call = continuation(request, context);

        var wrappedResponseAsync = WrapUnaryResponse(
            call.ResponseAsync, testInfo.Value, label, uri, serviceName, traceId, requestResponseId, effectiveVerbosity);

        return new AsyncUnaryCall<TResponse>(
            wrappedResponseAsync,
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return continuation(request, context);
        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return continuation(request, context);

        var opInfo = Classify(context);
        var label = GrpcOperationClassifier.GetDiagramLabel(opInfo, effectiveVerbosity);
        var serviceName = ResolveServiceName(opInfo);
        var uri = BuildUri(opInfo, effectiveVerbosity);
        var requestContent = SerializeMessage(request, effectiveVerbosity);
        var headers = GetCallHeaders(context);

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        LogRequest(testInfo.Value, label, requestContent, uri, headers, serviceName, traceId, requestResponseId);

        try
        {
            var response = continuation(request, context);
            var responseContent = SerializeMessage(response, effectiveVerbosity);
            LogResponse(testInfo.Value, label, responseContent, uri, serviceName, traceId, requestResponseId, HttpStatusCode.OK);
            return response;
        }
        catch (RpcException ex)
        {
            LogResponse(testInfo.Value, label, $"{ex.StatusCode}: {ex.Message}", uri, serviceName,
                traceId, requestResponseId, MapGrpcStatusToHttp(ex.StatusCode));
            throw;
        }
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return continuation(request, context);
        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return continuation(request, context);

        var opInfo = Classify(context);
        var label = GrpcOperationClassifier.GetDiagramLabel(opInfo, effectiveVerbosity);
        var serviceName = ResolveServiceName(opInfo);
        var uri = BuildUri(opInfo, effectiveVerbosity);
        var requestContent = SerializeMessage(request, effectiveVerbosity);
        var headers = GetCallHeaders(context);

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        LogRequest(testInfo.Value, label, requestContent, uri, headers, serviceName, traceId, requestResponseId);

        var call = continuation(request, context);

        LogResponse(testInfo.Value, label, null, uri, serviceName, traceId, requestResponseId, HttpStatusCode.OK);

        return call;
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return continuation(context);
        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return continuation(context);

        var opInfo = Classify(context);
        var label = GrpcOperationClassifier.GetDiagramLabel(opInfo, effectiveVerbosity);
        var serviceName = ResolveServiceName(opInfo);
        var uri = BuildUri(opInfo, effectiveVerbosity);
        var headers = GetCallHeaders(context);

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        LogRequest(testInfo.Value, label, null, uri, headers, serviceName, traceId, requestResponseId);

        var call = continuation(context);

        LogResponse(testInfo.Value, label, null, uri, serviceName, traceId, requestResponseId, HttpStatusCode.OK);

        return call;
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return continuation(context);
        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return continuation(context);

        var opInfo = Classify(context);
        var label = GrpcOperationClassifier.GetDiagramLabel(opInfo, effectiveVerbosity);
        var serviceName = ResolveServiceName(opInfo);
        var uri = BuildUri(opInfo, effectiveVerbosity);
        var headers = GetCallHeaders(context);

        var traceId = Guid.NewGuid();
        var requestResponseId = Guid.NewGuid();

        LogRequest(testInfo.Value, label, null, uri, headers, serviceName, traceId, requestResponseId);

        var call = continuation(context);

        LogResponse(testInfo.Value, label, null, uri, serviceName, traceId, requestResponseId, HttpStatusCode.OK);

        return call;
    }

    private async Task<TResponse> WrapUnaryResponse<TResponse>(
        Task<TResponse> responseTask,
        (string Name, string Id) testInfo,
        string label, Uri uri, string serviceName,
        Guid traceId, Guid requestResponseId, GrpcTrackingVerbosity effectiveVerbosity)
    {
        try
        {
            var response = await responseTask;
            var responseContent = SerializeMessage(response, effectiveVerbosity);
            LogResponse(testInfo, label, responseContent, uri, serviceName, traceId, requestResponseId, HttpStatusCode.OK);
            return response;
        }
        catch (RpcException ex)
        {
            LogResponse(testInfo, label, $"{ex.StatusCode}: {ex.Message}", uri, serviceName,
                traceId, requestResponseId, MapGrpcStatusToHttp(ex.StatusCode));
            throw;
        }
    }

    private void LogRequest(
        (string Name, string Id) testInfo, string label, string? content,
        Uri uri, (string Key, string? Value)[] headers, string serviceName,
        Guid traceId, Guid requestResponseId)
    {
        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Name, testInfo.Id,
            label, content, uri,
            headers, serviceName, _options.CallingServiceName,
            RequestResponseType.Request, traceId, requestResponseId, false)
        {
            Phase = TestPhaseContext.Current
        });
    }

    private void LogResponse(
        (string Name, string Id) testInfo, string label, string? content,
        Uri uri, string serviceName,
        Guid traceId, Guid requestResponseId, HttpStatusCode statusCode)
    {
        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Name, testInfo.Id,
            label, content, uri,
            [], serviceName, _options.CallingServiceName,
            RequestResponseType.Response, traceId, requestResponseId, false,
            statusCode)
        {
            Phase = TestPhaseContext.Current
        });
    }

    private static GrpcOperationInfo Classify<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        return GrpcOperationClassifier.Classify(
            context.Method.Type,
            context.Method.FullName,
            context.Method.ServiceName,
            context.Method.Name);
    }

    private string ResolveServiceName(GrpcOperationInfo opInfo)
    {
        return _options.UseProtoServiceNameInDiagram
            ? opInfo.ServiceName ?? _options.ServiceName
            : _options.ServiceName;
    }

    private Uri BuildUri(GrpcOperationInfo opInfo, GrpcTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            GrpcTrackingVerbosity.Summarised =>
                new Uri($"grpc:///{opInfo.ServiceName ?? "service"}/"),
            GrpcTrackingVerbosity.Detailed =>
                new Uri($"grpc:///{opInfo.ServiceName ?? "service"}/{opInfo.MethodName ?? "method"}"),
            _ => // Raw
                new Uri($"grpc:///{opInfo.FullMethodName ?? (opInfo.ServiceName + "/" + opInfo.MethodName)}")
        };
    }

    private string? SerializeMessage<T>(T message, GrpcTrackingVerbosity verbosity)
    {
        if (verbosity == GrpcTrackingVerbosity.Summarised) return null;

        if (message is IMessage protoMsg)
            return JsonFormatter.Default.Format(protoMsg);

        return message?.ToString();
    }

    public static HttpStatusCode MapGrpcStatusToHttp(StatusCode grpcStatus)
    {
        return grpcStatus switch
        {
            StatusCode.OK => HttpStatusCode.OK,
            StatusCode.NotFound => HttpStatusCode.NotFound,
            StatusCode.PermissionDenied => HttpStatusCode.Forbidden,
            StatusCode.Unauthenticated => HttpStatusCode.Unauthorized,
            StatusCode.InvalidArgument => HttpStatusCode.BadRequest,
            StatusCode.DeadlineExceeded => HttpStatusCode.RequestTimeout,
            StatusCode.AlreadyExists => HttpStatusCode.Conflict,
            StatusCode.ResourceExhausted => (HttpStatusCode)429,
            StatusCode.Unavailable => HttpStatusCode.ServiceUnavailable,
            StatusCode.Unimplemented => HttpStatusCode.NotImplemented,
            StatusCode.Cancelled => HttpStatusCode.RequestTimeout,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static (string Key, string? Value)[] GetCallHeaders<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        if (context.Options.Headers is null) return [];

        return context.Options.Headers
            .Where(h => !h.IsBinary)
            .Select(h => (h.Key, (string?)h.Value))
            .ToArray();
    }
}
