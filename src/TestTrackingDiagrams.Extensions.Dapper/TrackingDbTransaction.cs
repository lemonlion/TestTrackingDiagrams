using System.Data;
using System.Data.Common;

namespace TestTrackingDiagrams;

public class TrackingDbTransaction : DbTransaction
{
    private readonly DbTransaction _inner;
    private readonly TrackingDbConnection _connection;

    public TrackingDbTransaction(DbTransaction inner, TrackingDbConnection connection)
    {
        _inner = inner;
        _connection = connection;
    }

    protected override DbConnection? DbConnection => _connection;
    public override IsolationLevel IsolationLevel => _inner.IsolationLevel;

    public override void Commit() => _inner.Commit();
    public override Task CommitAsync(CancellationToken cancellationToken = default) => _inner.CommitAsync(cancellationToken);
    public override void Rollback() => _inner.Rollback();
    public override Task RollbackAsync(CancellationToken cancellationToken = default) => _inner.RollbackAsync(cancellationToken);

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
