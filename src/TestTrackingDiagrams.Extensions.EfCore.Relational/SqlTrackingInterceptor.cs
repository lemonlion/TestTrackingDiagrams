using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

public class SqlTrackingInterceptor : DbCommandInterceptor, ITrackingComponent
{
    private readonly SqlTrackingInterceptorOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private int _invocationCount;

    // Maps each DbCommand instance to its correlated IDs so that
    // LogCommandExecuted can pair with the LogCommandExecuting entry.
    private readonly ConcurrentDictionary<DbCommand, (Guid TraceId, Guid RequestResponseId)> _pendingIds = new();

    public SqlTrackingInterceptor(SqlTrackingInterceptorOptions options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _options = options;
        _httpContextAccessor = httpContextAccessor;
        TrackingComponentRegistry.Register(this);
    }

    public string ComponentName => $"SqlTrackingInterceptor ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    // ─── Public methods for direct testing ──────────────────────

    public void LogCommandExecuting(DbCommand command)
    {
        Interlocked.Increment(ref _invocationCount);

        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return;

        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);
        var sqlOp = SqlOperationClassifier.Classify(command.CommandText, command.CommandType);

        if (effectiveVerbosity == SqlTrackingVerbosity.Summarised && sqlOp.Operation == SqlOperation.Other)
            return;

        var testInfo = GetTestInfo();
        if (testInfo is null)
            return;

        var requestResponseId = Guid.NewGuid();
        var traceId = Guid.NewGuid();

        // Store the IDs so LogCommandExecuted can retrieve them
        _pendingIds[command] = (traceId, requestResponseId);

        var label = SqlOperationClassifier.GetDiagramLabel(sqlOp, effectiveVerbosity);

        OneOf<HttpMethod, string> method = effectiveVerbosity == SqlTrackingVerbosity.Raw
            ? SqlOperationClassifier.GetRawKeyword(command.CommandText) ?? "SQL"
            : label!;

        var requestUri = BuildUri(command, sqlOp, effectiveVerbosity);
        var content = effectiveVerbosity == SqlTrackingVerbosity.Summarised ? null : command.CommandText;

        var log = new RequestResponseLog(
            testInfo.Value.Name,
            testInfo.Value.Id,
            method,
            content,
            requestUri,
            [],
            _options.ServiceName,
            _options.CallingServiceName,
            RequestResponseType.Request,
            traceId,
            requestResponseId,
            false,
            DependencyCategory: "SQL"
        )
        {
            Phase = TestPhaseContext.Current
        };

        log.AttachVariants(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity,
            v => BuildVariant(command, sqlOp, v));

        RequestResponseLogger.Log(log);
    }

    public void LogCommandExecuted(DbCommand command, int? rowsAffected = null)
    {
        if (!PhaseConfiguration.ShouldTrack(_options.TrackDuringSetup, _options.TrackDuringAction))
            return;

        var effectiveVerbosity = PhaseConfiguration.GetEffectiveVerbosity(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity);
        var sqlOp = SqlOperationClassifier.Classify(command.CommandText, command.CommandType);

        if (effectiveVerbosity == SqlTrackingVerbosity.Summarised && sqlOp.Operation == SqlOperation.Other)
            return;

        var testInfo = GetTestInfo();
        if (testInfo is null)
            return;

        // Retrieve the IDs from the executing phase, or create new ones as fallback
        if (!_pendingIds.TryRemove(command, out var ids))
            ids = (Guid.NewGuid(), Guid.NewGuid());

        var label = SqlOperationClassifier.GetDiagramLabel(sqlOp, effectiveVerbosity);

        OneOf<HttpMethod, string> method = effectiveVerbosity == SqlTrackingVerbosity.Raw
            ? SqlOperationClassifier.GetRawKeyword(command.CommandText) ?? "SQL"
            : label!;

        var requestUri = BuildUri(command, sqlOp, effectiveVerbosity);
        var content = effectiveVerbosity == SqlTrackingVerbosity.Summarised
            ? null
            : rowsAffected.HasValue ? $"{rowsAffected.Value} rows affected" : null;

        var log = new RequestResponseLog(
            testInfo.Value.Name,
            testInfo.Value.Id,
            method,
            content,
            requestUri,
            [],
            _options.ServiceName,
            _options.CallingServiceName,
            RequestResponseType.Response,
            ids.TraceId,
            ids.RequestResponseId,
            false,
            (OneOf<System.Net.HttpStatusCode, string>)"OK",
            DependencyCategory: "SQL"
        )
        {
            Phase = TestPhaseContext.Current
        };

        log.AttachVariants(_options.Verbosity, _options.SetupVerbosity, _options.ActionVerbosity,
            v => BuildResponseVariant(command, sqlOp, v, rowsAffected));

        RequestResponseLogger.Log(log);
    }

    // ─── EF Core DbCommandInterceptor overrides ────────────────

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        LogCommandExecuting(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogCommandExecuted(command);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        LogCommandExecuting(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogCommandExecuted(command, result);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        LogCommandExecuting(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        LogCommandExecuted(command);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        LogCommandExecuting(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        LogCommandExecuted(command);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        LogCommandExecuting(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        LogCommandExecuted(command, result);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        LogCommandExecuting(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        LogCommandExecuted(command);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    // ─── Test identity resolution ────────────────────────────────

    private (string Name, string Id)? GetTestInfo()
        => TestInfoResolver.Resolve(_httpContextAccessor, _options.CurrentTestInfoFetcher);

    // ─── URI construction ──────────────────────────────────────

    private static Uri BuildUri(DbCommand command, SqlOperationInfo op, SqlTrackingVerbosity verbosity)
    {
        var database = command.Connection?.Database ?? "unknown";
        // SQL Server uses comma notation for ports (e.g. "127.0.0.1,1433")
        // but Uri requires colon notation (e.g. "127.0.0.1:1433").
        var dataSource = (command.Connection?.DataSource ?? "localhost").Replace(',', ':');

        return verbosity switch
        {
            SqlTrackingVerbosity.Raw => new Uri($"sql://{dataSource}/{database}"),
            SqlTrackingVerbosity.Summarised => op.TableName is not null
                ? new Uri($"sql://{database}/{op.TableName}")
                : new Uri($"sql://{database}"),
            _ => op.TableName is not null // Detailed
                ? new Uri($"sql://{dataSource}/{database}/{op.TableName}")
                : new Uri($"sql://{dataSource}/{database}"),
        };
    }

    // ─── Phase variant helpers ──────────────────────────────────

    private static PhaseVariant BuildVariant(DbCommand command, SqlOperationInfo sqlOp, SqlTrackingVerbosity verbosity)
    {
        var skip = verbosity == SqlTrackingVerbosity.Summarised && sqlOp.Operation == SqlOperation.Other;
        var label = SqlOperationClassifier.GetDiagramLabel(sqlOp, verbosity);
        OneOf<HttpMethod, string> method = verbosity == SqlTrackingVerbosity.Raw
            ? SqlOperationClassifier.GetRawKeyword(command.CommandText) ?? "SQL"
            : label!;
        var uri = BuildUri(command, sqlOp, verbosity);
        var content = verbosity == SqlTrackingVerbosity.Summarised ? null : command.CommandText;

        return new PhaseVariant(method, uri, content, [], skip);
    }

    private static PhaseVariant BuildResponseVariant(DbCommand command, SqlOperationInfo sqlOp, SqlTrackingVerbosity verbosity, int? rowsAffected)
    {
        var skip = verbosity == SqlTrackingVerbosity.Summarised && sqlOp.Operation == SqlOperation.Other;
        var label = SqlOperationClassifier.GetDiagramLabel(sqlOp, verbosity);
        OneOf<HttpMethod, string> method = verbosity == SqlTrackingVerbosity.Raw
            ? SqlOperationClassifier.GetRawKeyword(command.CommandText) ?? "SQL"
            : label!;
        var uri = BuildUri(command, sqlOp, verbosity);
        var content = verbosity == SqlTrackingVerbosity.Summarised
            ? null
            : rowsAffected.HasValue ? $"{rowsAffected.Value} rows affected" : null;

        return new PhaseVariant(method, uri, content, [], skip);
    }
}
