using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Spanner;

public class TrackingSpannerConnection : DbConnection, ITrackingComponent
{
    private readonly DbConnection _inner;
    private readonly SpannerTracker _tracker;
    private readonly SpannerTrackingOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public TrackingSpannerConnection(DbConnection inner, SpannerTrackingOptions options, IHttpContextAccessor? httpContextAccessor = null)
    {
        _inner = inner;
        _options = options;
        _httpContextAccessor = httpContextAccessor;
        _tracker = new SpannerTracker(options, httpContextAccessor);
    }

    public DbConnection InnerConnection => _inner;
    internal SpannerTracker Tracker => _tracker;

    public string ComponentName => _tracker.ComponentName;
    public bool WasInvoked => _tracker.WasInvoked;
    public int InvocationCount => _tracker.InvocationCount;

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
        return new TrackingSpannerCommand(innerCommand, this, _options, _httpContextAccessor);
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        var innerTx = _inner.BeginTransaction(isolationLevel);
        return new TrackingSpannerTransaction(innerTx, this);
    }

    protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(
        IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        var innerTx = await _inner.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new TrackingSpannerTransaction(innerTx, this);
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
