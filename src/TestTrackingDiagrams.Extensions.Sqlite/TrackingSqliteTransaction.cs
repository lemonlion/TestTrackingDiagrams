using System.Data.Common;
using TestTrackingDiagrams.Sql;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Sqlite;

public class TrackingSqliteTransaction : DbTransaction
{
    private readonly DbTransaction _inner;
    private readonly TrackingSqliteConnection _connection;

    public TrackingSqliteTransaction(DbTransaction inner, TrackingSqliteConnection connection)
    {
        _inner = inner;
        _connection = connection;

        // Log BEGIN TRANSACTION
        var ids = _connection.Tracker.DoLogRequest("BEGIN TRANSACTION", _connection.InnerConnection.DataSource, _connection.InnerConnection.Database);
        if (ids is not null)
            _connection.Tracker.DoLogResponse(ids.Value.TraceId, ids.Value.RequestResponseId);
    }

    protected override DbConnection DbConnection => _connection;

    public override System.Data.IsolationLevel IsolationLevel => _inner.IsolationLevel;

    public override void Commit()
    {
        _inner.Commit();
        var ids = _connection.Tracker.DoLogRequest("COMMIT", _connection.InnerConnection.DataSource, _connection.InnerConnection.Database);
        if (ids is not null)
            _connection.Tracker.DoLogResponse(ids.Value.TraceId, ids.Value.RequestResponseId);
    }

    public override void Rollback()
    {
        _inner.Rollback();
        var ids = _connection.Tracker.DoLogRequest("ROLLBACK", _connection.InnerConnection.DataSource, _connection.InnerConnection.Database);
        if (ids is not null)
            _connection.Tracker.DoLogResponse(ids.Value.TraceId, ids.Value.RequestResponseId);
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
