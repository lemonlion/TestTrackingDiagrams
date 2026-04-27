using System.Data;
using TestTrackingDiagrams.Tests.Dapper.Fakes;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Dapper;

public class TrackingDbConnectionTests : IDisposable
{
    private readonly FakeDbConnection _fakeConnection = new();
    private readonly DapperTrackingOptions _options = new();

    public TrackingDbConnectionTests()
    {
        TrackingComponentRegistry.Clear();
    }

    public void Dispose()
    {
        TrackingComponentRegistry.Clear();
    }

    [Fact]
    public void CreateCommand_returns_TrackingDbCommand()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        var cmd = conn.CreateCommand();
        Assert.IsType<TrackingDbCommand>(cmd);
    }

    [Fact]
    public void Open_delegates_to_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        conn.Open();
        Assert.True(_fakeConnection.WasOpened);
        Assert.Equal(ConnectionState.Open, conn.State);
    }

    [Fact]
    public void Close_delegates_to_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        conn.Open();
        conn.Close();
        Assert.True(_fakeConnection.WasClosed);
    }

    [Fact]
    public void ConnectionString_delegates_to_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        conn.ConnectionString = "Server=test";
        Assert.Equal("Server=test", conn.ConnectionString);
        Assert.Equal("Server=test", _fakeConnection.ConnectionString);
    }

    [Fact]
    public void Database_delegates_to_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        Assert.Equal("TestDb", conn.Database);
    }

    [Fact]
    public void DataSource_delegates_to_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        Assert.Equal("localhost", conn.DataSource);
    }

    [Fact]
    public void State_delegates_to_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        Assert.Equal(ConnectionState.Closed, conn.State);
        conn.Open();
        Assert.Equal(ConnectionState.Open, conn.State);
    }

    [Fact]
    public void ChangeDatabase_delegates_to_inner()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        conn.ChangeDatabase("OtherDb");
        Assert.Equal("OtherDb", _fakeConnection.ChangedDatabase);
    }

    [Fact]
    public void BeginTransaction_returns_TrackingDbTransaction()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);
        Assert.IsType<TrackingDbTransaction>(tx);
    }

    [Fact]
    public void Dispose_disposes_inner()
    {
        var conn = new TrackingDbConnection(_fakeConnection, _options);
        conn.Dispose();
        Assert.True(_fakeConnection.WasDisposed);
    }

    [Fact]
    public void InnerConnection_exposes_underlying_connection()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        Assert.Same(_fakeConnection, conn.InnerConnection);
    }

    // ─── ITrackingComponent ─────────────────────────────────────

    [Fact]
    public void Implements_ITrackingComponent()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        Assert.IsAssignableFrom<ITrackingComponent>(conn);
    }

    [Fact]
    public void WasInvoked_is_false_before_any_commands()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        Assert.False(conn.WasInvoked);
    }

    [Fact]
    public void WasInvoked_is_true_after_command()
    {
        _options.CurrentTestInfoFetcher = () => ("Test", "test-id");
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1";
        cmd.ExecuteScalar();
        Assert.True(conn.WasInvoked);
    }

    [Fact]
    public void InvocationCount_starts_at_zero()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        Assert.Equal(0, conn.InvocationCount);
    }

    [Fact]
    public void InvocationCount_increases_with_each_command()
    {
        _options.CurrentTestInfoFetcher = () => ("Test", "test-id");
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1";

        cmd.ExecuteScalar();
        Assert.Equal(1, conn.InvocationCount);

        cmd.ExecuteScalar();
        Assert.Equal(2, conn.InvocationCount);
    }

    [Fact]
    public void ComponentName_matches_service_name()
    {
        _options.ServiceName = "MyDatabase";
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        Assert.Equal("TrackingDbConnection (MyDatabase)", conn.ComponentName);
    }

    [Fact]
    public void Constructor_auto_registers_with_TrackingComponentRegistry()
    {
        using var conn = new TrackingDbConnection(_fakeConnection, _options);
        var registered = TrackingComponentRegistry.GetRegisteredComponents();
        Assert.Contains(registered, c => ReferenceEquals(c, conn));
    }
}
