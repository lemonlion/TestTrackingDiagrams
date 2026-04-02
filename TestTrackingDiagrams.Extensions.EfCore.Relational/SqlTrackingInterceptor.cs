using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

public class SqlTrackingInterceptor : DbCommandInterceptor
{
    private readonly SqlTrackingInterceptorOptions _options;

    public SqlTrackingInterceptor(SqlTrackingInterceptorOptions options)
    {
        _options = options;
    }

    // ─── Public methods for direct testing ──────────────────────

    public void LogCommandExecuting(DbCommand command)
    {
        var sqlOp = SqlOperationClassifier.Classify(command.CommandText, command.CommandType);

        if (_options.Verbosity == SqlTrackingVerbosity.Summarised && sqlOp.Operation == SqlOperation.Other)
            return;

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return;

        var requestResponseId = Guid.NewGuid();
        var traceId = Guid.NewGuid();

        var label = SqlOperationClassifier.GetDiagramLabel(sqlOp, _options.Verbosity);

        OneOf<HttpMethod, string> method = _options.Verbosity == SqlTrackingVerbosity.Raw
            ? SqlOperationClassifier.GetRawKeyword(command.CommandText) ?? "SQL"
            : label!;

        var requestUri = BuildUri(command, sqlOp, _options.Verbosity);
        var content = _options.Verbosity == SqlTrackingVerbosity.Summarised ? null : command.CommandText;

        RequestResponseLogger.Log(new RequestResponseLog(
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
            false
        ));
    }

    public void LogCommandExecuted(DbCommand command, int? rowsAffected = null)
    {
        var sqlOp = SqlOperationClassifier.Classify(command.CommandText, command.CommandType);

        if (_options.Verbosity == SqlTrackingVerbosity.Summarised && sqlOp.Operation == SqlOperation.Other)
            return;

        var testInfo = _options.CurrentTestInfoFetcher?.Invoke();
        if (testInfo is null)
            return;

        var requestResponseId = Guid.NewGuid();
        var traceId = Guid.NewGuid();

        var label = SqlOperationClassifier.GetDiagramLabel(sqlOp, _options.Verbosity);

        OneOf<HttpMethod, string> method = _options.Verbosity == SqlTrackingVerbosity.Raw
            ? SqlOperationClassifier.GetRawKeyword(command.CommandText) ?? "SQL"
            : label!;

        var requestUri = BuildUri(command, sqlOp, _options.Verbosity);
        var content = _options.Verbosity == SqlTrackingVerbosity.Summarised
            ? null
            : rowsAffected.HasValue ? $"{rowsAffected.Value} rows affected" : null;

        RequestResponseLogger.Log(new RequestResponseLog(
            testInfo.Value.Name,
            testInfo.Value.Id,
            method,
            content,
            requestUri,
            [],
            _options.ServiceName,
            _options.CallingServiceName,
            RequestResponseType.Response,
            traceId,
            requestResponseId,
            false,
            (OneOf<System.Net.HttpStatusCode, string>)"OK"
        ));
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

    // ─── URI construction ──────────────────────────────────────

    private static Uri BuildUri(DbCommand command, SqlOperationInfo op, SqlTrackingVerbosity verbosity)
    {
        var database = command.Connection?.Database ?? "unknown";
        var dataSource = command.Connection?.DataSource ?? "localhost";

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
}
