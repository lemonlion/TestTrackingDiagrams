using System.Net;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Redis;

public class RedisTracker : ITrackingComponent
{
    private readonly RedisTrackingDatabaseOptions _options;
    private readonly string _endpoint;
    private int _invocationCount;

    public RedisTracker(RedisTrackingDatabaseOptions options, string endpoint = "localhost")
    {
        _options = options;
        _endpoint = endpoint;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"RedisTracker ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    public (Guid RequestResponseId, Guid TraceId) LogRedisRequest(string command, string? key, int db, string? content)
    {
        Interlocked.Increment(ref _invocationCount);

        var op = RedisOperationClassifier.Classify(command, hasResult: false, key, db);

        if (_options.Verbosity == RedisTrackingVerbosity.Summarised && op.Operation == RedisOperation.Other)
            return (Guid.Empty, Guid.Empty);

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return (Guid.Empty, Guid.Empty);

        var requestResponseId = Guid.NewGuid();
        var traceId = Guid.NewGuid();

        // Request label never includes hit/miss — that's only known after the response
        var requestOp = op with { CacheResult = RedisCacheResult.None };
        var label = RedisOperationClassifier.GetDiagramLabel(requestOp, _options.Verbosity);

        OneOf<HttpMethod, string> method = _options.Verbosity == RedisTrackingVerbosity.Raw
            ? command.ToUpperInvariant()
            : label ?? op.Operation.ToString();

        var requestUri = BuildUri(key, db, _options.Verbosity);
        var logContent = _options.Verbosity == RedisTrackingVerbosity.Summarised ? null : content;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name,
            testInfo.Value.Id,
            method,
            logContent,
            requestUri,
            [],
            _options.ServiceName,
            _options.CallingServiceName,
            RequestResponseType.Request,
            traceId,
            requestResponseId,
            false,
            DependencyCategory: "Redis"
        ));

        return (requestResponseId, traceId);
    }

    public void LogRedisResponse(string command, string? key, int db, bool hasResult, Guid requestResponseId, Guid traceId, string? content)
    {
        var op = RedisOperationClassifier.Classify(command, hasResult, key, db);

        if (_options.Verbosity == RedisTrackingVerbosity.Summarised && op.Operation == RedisOperation.Other)
            return;

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return;

        var label = RedisOperationClassifier.GetDiagramLabel(op, _options.Verbosity);

        OneOf<HttpMethod, string> method = _options.Verbosity == RedisTrackingVerbosity.Raw
            ? command.ToUpperInvariant()
            : label ?? op.Operation.ToString();

        var requestUri = BuildUri(key, db, _options.Verbosity);
        var logContent = _options.Verbosity == RedisTrackingVerbosity.Summarised ? null : content;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name,
            testInfo.Value.Id,
            method,
            logContent,
            requestUri,
            [],
            _options.ServiceName,
            _options.CallingServiceName,
            RequestResponseType.Response,
            traceId,
            requestResponseId,
            false,
            (OneOf<HttpStatusCode, string>)"OK",
            DependencyCategory: "Redis"
        ));
    }

    private Uri BuildUri(string? key, int db, RedisTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            RedisTrackingVerbosity.Raw => key is not null
                ? new Uri($"redis://{_endpoint}/{db}/{key}")
                : new Uri($"redis://{_endpoint}/{db}"),
            RedisTrackingVerbosity.Detailed => key is not null
                ? new Uri($"redis://db{db}/{key}")
                : new Uri($"redis://db{db}/"),
            _ => new Uri($"redis://db{db}/"), // Summarised
        };
    }
}
