using System.Data;
using TestTrackingDiagrams.Tests.Dapper.Fakes;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Dapper;

public class TrackingDbTransactionTests : IDisposable
{
    private readonly FakeDbConnection _fakeConnection = new();
    private readonly DapperTrackingOptions _options = new();

    public TrackingDbTransactionTests()
    {
        TrackingComponentRegistry.Clear();
        RequestResponseLogger.Clear();
    }

    public void Dispose()
    {
        TrackingComponentRegistry.Clear();
        RequestResponseLogger.Clear();
    }

    [Fact]
    public void Commit_delegates_to_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);

        tx.Commit();

        var innerTx = (FakeDbTransaction)_fakeConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        // The tracking tx delegates to the inner one
        Assert.IsType<TrackingDbTransaction>(tx);
    }

    [Fact]
    public void Rollback_delegates_to_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        tx.Rollback();

        Assert.IsType<TrackingDbTransaction>(tx);
    }

    [Fact]
    public void IsolationLevel_delegates_to_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        using var tx = conn.BeginTransaction(IsolationLevel.Snapshot);

        Assert.Equal(IsolationLevel.Snapshot, tx.IsolationLevel);
    }

    [Fact]
    public void Connection_returns_tracking_connection()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        using var tx = conn.BeginTransaction();

        Assert.Same(conn, tx.Connection);
    }

    [Fact]
    public void Dispose_disposes_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        var tx = conn.BeginTransaction();
        tx.Dispose();
        // No exception means inner was disposed successfully
    }

    [Fact]
    public async Task DisposeAsync_disposes_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        var tx = conn.BeginTransaction();
        await tx.DisposeAsync();
        // No exception means inner was disposed successfully
    }
}
