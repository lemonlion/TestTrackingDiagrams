using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace TestTrackingDiagrams.Tests.Dapper.Fakes;

public class FakeDbConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;

    [AllowNull]
    public override string ConnectionString { get; set; } = "";
    public override string Database { get; } = "TestDb";
    public override string DataSource { get; } = "localhost";
    public override string ServerVersion { get; } = "1.0";
    public override ConnectionState State => _state;

    public FakeDbCommand? LastCreatedCommand { get; private set; }
    public bool WasOpened { get; private set; }
    public bool WasClosed { get; private set; }
    public bool WasDisposed { get; private set; }
    public string? ChangedDatabase { get; private set; }
    public IsolationLevel? LastBeginTransactionIsolationLevel { get; private set; }

    public override void Open()
    {
        _state = ConnectionState.Open;
        WasOpened = true;
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        Open();
        return Task.CompletedTask;
    }

    public override void Close()
    {
        _state = ConnectionState.Closed;
        WasClosed = true;
    }

    public override void ChangeDatabase(string databaseName) => ChangedDatabase = databaseName;

    protected override DbCommand CreateDbCommand()
    {
        var cmd = new FakeDbCommand { Connection = this };
        LastCreatedCommand = cmd;
        return cmd;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        LastBeginTransactionIsolationLevel = isolationLevel;
        return new FakeDbTransaction(this, isolationLevel);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) WasDisposed = true;
        base.Dispose(disposing);
    }
}
