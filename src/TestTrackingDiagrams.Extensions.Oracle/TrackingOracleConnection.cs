using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Oracle.ManagedDataAccess.Client;
using TestTrackingDiagrams.Sql;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Oracle;

/// <summary>
/// Decorator wrapping an <see cref="OracleConnection"/> to intercept and track
/// all SQL operations for test diagram generation.
/// </summary>
public class TrackingOracleConnection : DbConnection, ITrackingComponent
{
    private readonly OracleConnection _inner;
    private readonly OracleTrackingOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly SqlDiagnosticTrackerForOracleWrapping _tracker;
    private int _invocationCount;

    public TrackingOracleConnection(OracleConnection inner, OracleTrackingOptions options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _inner = inner;
        _options = options;
        _httpContextAccessor = httpContextAccessor ?? options.HttpContextAccessor;
        _tracker = new SqlDiagnosticTrackerForOracleWrapping(options, _httpContextAccessor);
        TrackingComponentRegistry.Register(this);
    }

    public OracleConnection InnerConnection => _inner;

    public string ComponentName => $"TrackingOracleConnection ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    internal void IncrementInvocationCount() => Interlocked.Increment(ref _invocationCount);
    internal SqlDiagnosticTrackerForOracleWrapping Tracker => _tracker;
    internal OracleTrackingOptions Options => _options;

    [AllowNull]
    public override string ConnectionString
    {
        get => _inner.ConnectionString!;
        set => _inner.ConnectionString = value;
    }

    public override string Database => _inner.Database;
    public override string DataSource => _inner.DataSource!;
    public override string ServerVersion => _inner.ServerVersion;
    public override ConnectionState State => _inner.State;

    public override void Open() => _inner.Open();
    public override Task OpenAsync(CancellationToken cancellationToken) => _inner.OpenAsync(cancellationToken);
    public override void Close() => _inner.Close();
    public override Task CloseAsync() => _inner.CloseAsync();
    public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

    protected override DbCommand CreateDbCommand()
    {
        var innerCommand = _inner.CreateCommand();
        return new TrackingOracleCommand(innerCommand, this);
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        var innerTx = _inner.BeginTransaction(isolationLevel);
        return new TrackingOracleTransaction(innerTx, this);
    }

    protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(
        IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        var innerTx = await _inner.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new TrackingOracleTransaction(innerTx, this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync();
        await base.DisposeAsync();
    }
}

internal sealed class SqlDiagnosticTrackerForOracleWrapping : SqlDiagnosticTracker
{
    public SqlDiagnosticTrackerForOracleWrapping(SqlTrackingOptionsBase options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options, httpContextAccessor) { }

    public override string ComponentName => "SqlDiagnosticTrackerForOracleWrapping";

    public (Guid TraceId, Guid RequestResponseId)? DoLogRequest(string? commandText, string? dataSource, string? database,
        CommandType commandType = CommandType.Text, string? parameters = null)
        => LogRequest(commandText, dataSource, database, commandType, parameters);

    public void DoLogResponse(Guid traceId, Guid requestResponseId, int? rowsAffected = null, Exception? exception = null)
        => LogResponse(traceId, requestResponseId, rowsAffected, exception);
}
