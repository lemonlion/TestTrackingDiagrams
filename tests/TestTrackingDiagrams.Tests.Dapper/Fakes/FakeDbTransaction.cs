using System.Data;
using System.Data.Common;

namespace TestTrackingDiagrams.Tests.Dapper.Fakes;

public class FakeDbTransaction : DbTransaction
{
    private readonly DbConnection _connection;

    public FakeDbTransaction(DbConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        IsolationLevel = isolationLevel;
    }

    protected override DbConnection? DbConnection => _connection;
    public override IsolationLevel IsolationLevel { get; }

    public bool WasCommitted { get; private set; }
    public bool WasRolledBack { get; private set; }
    public bool WasDisposed { get; private set; }

    public override void Commit() => WasCommitted = true;
    public override void Rollback() => WasRolledBack = true;

    protected override void Dispose(bool disposing)
    {
        if (disposing) WasDisposed = true;
        base.Dispose(disposing);
    }
}
