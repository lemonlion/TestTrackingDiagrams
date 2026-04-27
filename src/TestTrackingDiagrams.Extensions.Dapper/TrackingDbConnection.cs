using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;

public class TrackingDbConnection : DbConnection, ITrackingComponent
{
    private readonly DbConnection _inner;
    private readonly DapperTrackingOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private int _invocationCount;

    public TrackingDbConnection(DbConnection inner, DapperTrackingOptions options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _inner = inner;
        _options = options;
        _httpContextAccessor = httpContextAccessor ?? options.HttpContextAccessor;
        TrackingComponentRegistry.Register(this);
    }

    public DbConnection InnerConnection => _inner;
    internal DapperTrackingOptions Options => _options;

    public string ComponentName => $"TrackingDbConnection ({_options.ServiceName})";
    public bool WasInvoked => _invocationCount > 0;
    public int InvocationCount => _invocationCount;

    internal void IncrementInvocationCount() => Interlocked.Increment(ref _invocationCount);

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
        return new TrackingDbCommand(innerCommand, this, _options, _httpContextAccessor);
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        var innerTx = _inner.BeginTransaction(isolationLevel);
        return new TrackingDbTransaction(innerTx, this);
    }

    protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(
        IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        var innerTx = await _inner.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new TrackingDbTransaction(innerTx, this);
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
