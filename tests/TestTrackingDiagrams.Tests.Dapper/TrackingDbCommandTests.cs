using System.Data;
using TestTrackingDiagrams.Tests.Dapper.Fakes;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Dapper;

public class TrackingDbCommandTests : IDisposable
{
    private readonly FakeDbConnection _fakeConnection = new();
    private readonly DapperTrackingOptions _options;
    private readonly TrackingDbConnection _trackingConnection;

    public TrackingDbCommandTests()
    {
        TrackingComponentRegistry.Clear();
        RequestResponseLogger.Clear();

        _options = new DapperTrackingOptions
        {
            CurrentTestInfoFetcher = () => ("TestMethod", "test-123"),
            ServiceName = "TestDB",
            CallingServiceName = "TestCaller"
        };

        _trackingConnection = new TrackingDbConnection(_fakeConnection, _options);
    }

    public void Dispose()
    {
        _trackingConnection.Dispose();
        TrackingComponentRegistry.Clear();
        RequestResponseLogger.Clear();
    }

    private TrackingDbCommand CreateCommand(string sql, CommandType type = CommandType.Text)
    {
        var cmd = (TrackingDbCommand)_trackingConnection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = type;
        return cmd;
    }

    // ─── Logging ────────────────────────────────────────────────

    [Fact]
    public void ExecuteReader_logs_request_and_response()
    {
        using var cmd = CreateCommand("SELECT * FROM Users");
        cmd.ExecuteReader();

        var logs = RequestResponseLogger.RequestAndResponseLogs;
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public async Task ExecuteReaderAsync_logs_request_and_response()
    {
        using var cmd = CreateCommand("SELECT * FROM Users");
        await cmd.ExecuteReaderAsync();

        var logs = RequestResponseLogger.RequestAndResponseLogs;
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void ExecuteNonQuery_logs_insert()
    {
        using var cmd = CreateCommand("INSERT INTO Users (Name) VALUES ('test')");
        cmd.ExecuteNonQuery();

        var logs = RequestResponseLogger.RequestAndResponseLogs;
        Assert.Equal(2, logs.Length);

        var request = logs[0];
        Assert.Equal("TestMethod", request.TestName);
        Assert.Equal("test-123", request.TestId);
        Assert.Equal("TestDB", request.ServiceName);
        Assert.Equal("TestCaller", request.CallerName);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_logs_correctly()
    {
        using var cmd = CreateCommand("UPDATE Users SET Name = 'test'");
        await cmd.ExecuteNonQueryAsync();

        var logs = RequestResponseLogger.RequestAndResponseLogs;
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void ExecuteScalar_logs_query()
    {
        using var cmd = CreateCommand("SELECT COUNT(*) FROM Users");
        cmd.ExecuteScalar();

        var logs = RequestResponseLogger.RequestAndResponseLogs;
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public async Task ExecuteScalarAsync_logs_correctly()
    {
        using var cmd = CreateCommand("SELECT COUNT(*) FROM Users");
        await cmd.ExecuteScalarAsync();

        var logs = RequestResponseLogger.RequestAndResponseLogs;
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void Request_and_response_share_trace_and_request_response_ids()
    {
        using var cmd = CreateCommand("SELECT * FROM Users");
        cmd.ExecuteReader();

        var logs = RequestResponseLogger.RequestAndResponseLogs;
        Assert.Equal(logs[0].TraceId, logs[1].TraceId);
        Assert.Equal(logs[0].RequestResponseId, logs[1].RequestResponseId);
    }

    [Fact]
    public void Response_includes_rows_affected_for_non_query()
    {
        var fakeCmd = new FakeDbCommand { NonQueryResult = 5 };
        using var cmd = new TrackingDbCommand(fakeCmd, _trackingConnection, _options);
        cmd.CommandText = "DELETE FROM Users WHERE Active = 0";
        cmd.ExecuteNonQuery();

        var logs = RequestResponseLogger.RequestAndResponseLogs;
        var response = logs[1];
        Assert.Equal("5 rows affected", response.Content);
    }

    // ─── No test info → no logging ──────────────────────────────

    [Fact]
    public void No_test_info_produces_no_logs()
    {
        _options.CurrentTestInfoFetcher = () => null;
        using var cmd = CreateCommand("SELECT * FROM Users");
        cmd.ExecuteReader();

        Assert.Empty(RequestResponseLogger.RequestAndResponseLogs);
    }

    [Fact]
    public void Null_test_info_fetcher_produces_no_logs()
    {
        _options.CurrentTestInfoFetcher = null;
        using var cmd = CreateCommand("SELECT * FROM Users");
        cmd.ExecuteReader();

        Assert.Empty(RequestResponseLogger.RequestAndResponseLogs);
    }

    // ─── Excluded operations ────────────────────────────────────

    [Fact]
    public void Excluded_operation_produces_no_logs()
    {
        _options.ExcludedOperations = [DapperOperation.Query];
        using var cmd = CreateCommand("SELECT * FROM Users");
        cmd.ExecuteReader();

        Assert.Empty(RequestResponseLogger.RequestAndResponseLogs);
    }

    // ─── Parameters ─────────────────────────────────────────────

    [Fact]
    public void LogParameters_true_includes_parameters_in_content()
    {
        _options.LogParameters = true;
        _options.Verbosity = DapperTrackingVerbosity.Raw;
        using var cmd = CreateCommand("SELECT * FROM Users WHERE Id = @Id");
        var param = cmd.CreateParameter();
        param.ParameterName = "@Id";
        param.Value = 42;
        cmd.Parameters.Add(param);

        cmd.ExecuteReader();

        var request = RequestResponseLogger.RequestAndResponseLogs[0];
        Assert.Contains("@Id=42", request.Content);
        Assert.Contains("-- Parameters:", request.Content);
    }

    [Fact]
    public void LogParameters_false_excludes_parameters_from_content()
    {
        _options.LogParameters = false;
        _options.Verbosity = DapperTrackingVerbosity.Raw;
        using var cmd = CreateCommand("SELECT * FROM Users WHERE Id = @Id");
        var param = cmd.CreateParameter();
        param.ParameterName = "@Id";
        param.Value = 42;
        cmd.Parameters.Add(param);

        cmd.ExecuteReader();

        var request = RequestResponseLogger.RequestAndResponseLogs[0];
        Assert.DoesNotContain("-- Parameters:", request.Content);
    }

    // ─── Verbosity levels ───────────────────────────────────────

    [Fact]
    public void Raw_verbosity_includes_full_sql_as_method()
    {
        _options.Verbosity = DapperTrackingVerbosity.Raw;
        using var cmd = CreateCommand("SELECT * FROM Users WHERE Id = 1");
        cmd.ExecuteReader();

        var request = RequestResponseLogger.RequestAndResponseLogs[0];
        Assert.Equal("SELECT * FROM Users WHERE Id = 1", request.Method.Value?.ToString());
    }

    [Fact]
    public void Detailed_verbosity_includes_table_in_method()
    {
        _options.Verbosity = DapperTrackingVerbosity.Detailed;
        using var cmd = CreateCommand("SELECT * FROM Users WHERE Id = 1");
        cmd.ExecuteReader();

        var request = RequestResponseLogger.RequestAndResponseLogs[0];
        Assert.Equal("SELECT FROM Users", request.Method.Value?.ToString());
    }

    [Fact]
    public void Summarised_verbosity_uses_keyword_only()
    {
        _options.Verbosity = DapperTrackingVerbosity.Summarised;
        using var cmd = CreateCommand("SELECT * FROM Users WHERE Id = 1");
        cmd.ExecuteReader();

        var request = RequestResponseLogger.RequestAndResponseLogs[0];
        Assert.Equal("SELECT", request.Method.Value?.ToString());
    }

    [Fact]
    public void Summarised_verbosity_has_null_content()
    {
        _options.Verbosity = DapperTrackingVerbosity.Summarised;
        using var cmd = CreateCommand("SELECT * FROM Users WHERE Id = 1");
        cmd.ExecuteReader();

        var response = RequestResponseLogger.RequestAndResponseLogs[1];
        Assert.Null(response.Content);
    }

    // ─── URI construction ───────────────────────────────────────

    [Fact]
    public void Detailed_uri_includes_datasource_database_and_table()
    {
        _options.Verbosity = DapperTrackingVerbosity.Detailed;
        using var cmd = CreateCommand("SELECT * FROM Users");
        cmd.ExecuteReader();

        var request = RequestResponseLogger.RequestAndResponseLogs[0];
        Assert.Contains("localhost", request.Uri.ToString());
        Assert.Contains("TestDb", request.Uri.ToString());
        Assert.Contains("Users", request.Uri.ToString());
    }

    [Fact]
    public void Summarised_uri_includes_database_and_table()
    {
        _options.Verbosity = DapperTrackingVerbosity.Summarised;
        using var cmd = CreateCommand("SELECT * FROM Users");
        cmd.ExecuteReader();

        var request = RequestResponseLogger.RequestAndResponseLogs[0];
        Assert.Contains("TestDb", request.Uri.ToString());
        Assert.Contains("Users", request.Uri.ToString());
    }

    [Fact]
    public void Raw_uri_includes_datasource_and_database()
    {
        _options.Verbosity = DapperTrackingVerbosity.Raw;
        using var cmd = CreateCommand("SELECT * FROM Users");
        cmd.ExecuteReader();

        var request = RequestResponseLogger.RequestAndResponseLogs[0];
        Assert.Contains("localhost", request.Uri.ToString());
        Assert.Contains("TestDb", request.Uri.ToString());
    }

    // ─── Delegation ─────────────────────────────────────────────

    [Fact]
    public void CommandText_delegates_to_inner()
    {
        using var cmd = CreateCommand("SELECT 1");
        Assert.Equal("SELECT 1", cmd.CommandText);
        cmd.CommandText = "SELECT 2";
        Assert.Equal("SELECT 2", cmd.CommandText);
    }

    [Fact]
    public void CommandType_delegates_to_inner()
    {
        using var cmd = CreateCommand("sp_Test", CommandType.StoredProcedure);
        Assert.Equal(CommandType.StoredProcedure, cmd.CommandType);
    }

    [Fact]
    public void CreateParameter_delegates_to_inner()
    {
        using var cmd = CreateCommand("SELECT 1");
        var param = cmd.CreateParameter();
        Assert.NotNull(param);
    }

    [Fact]
    public void Dispose_disposes_inner()
    {
        var innerCmd = new FakeDbCommand();
        var cmd = new TrackingDbCommand(innerCmd, _trackingConnection, _options);
        cmd.Dispose();
        Assert.True(innerCmd.WasDisposed);
    }

    // ─── LogSqlText option ──────────────────────────────────────

    [Fact]
    public void LogSqlText_false_does_not_include_sql_at_detailed()
    {
        _options.Verbosity = DapperTrackingVerbosity.Detailed;
        _options.LogSqlText = false;
        using var cmd = CreateCommand("SELECT * FROM Users WHERE Secret = 'password'");
        cmd.ExecuteReader();

        var request = RequestResponseLogger.RequestAndResponseLogs[0];
        Assert.Null(request.Content);
    }

    [Fact]
    public void LogSqlText_true_includes_sql_at_detailed()
    {
        _options.Verbosity = DapperTrackingVerbosity.Detailed;
        _options.LogSqlText = true;
        using var cmd = CreateCommand("SELECT * FROM Users");
        cmd.ExecuteReader();

        var request = RequestResponseLogger.RequestAndResponseLogs[0];
        Assert.Equal("SELECT * FROM Users", request.Content);
    }

    // ─── Invocation count still increments even when test info is null ────

    [Fact]
    public void InvocationCount_increments_even_without_test_info()
    {
        _options.CurrentTestInfoFetcher = null;
        using var cmd = CreateCommand("SELECT 1");
        cmd.ExecuteScalar();
        Assert.Equal(1, _trackingConnection.InvocationCount);
    }
}
