using System.Data;
using System.Data.Common;
using TestTrackingDiagrams.Extensions.EfCore.Relational;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.EfCore.Relational;

public class SqlTrackingInterceptorTests : IDisposable
{
    // ─── Test infrastructure ────────────────────────────────────

    private readonly string _testId = Guid.NewGuid().ToString();

    private RequestResponseLog[] GetLogsFromThisTest()
    {
        return RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == _testId)
            .ToArray();
    }

    private SqlTrackingInterceptorOptions MakeOptions(
        SqlTrackingVerbosity verbosity = SqlTrackingVerbosity.Detailed,
        string serviceName = "Database",
        string callerName = "TestCaller") => new()
    {
        ServiceName = serviceName,
        CallingServiceName = callerName,
        Verbosity = verbosity,
        CurrentTestInfoFetcher = () => ("My Test", _testId),
    };

    private static StubDbCommand MakeSelectCommand(string database = "MyDb", string dataSource = "localhost")
    {
        return new StubDbCommand("SELECT [u].[Id] FROM [dbo].[Users] AS [u]", database, dataSource);
    }

    private static StubDbCommand MakeInsertCommand(string database = "MyDb", string dataSource = "localhost")
    {
        return new StubDbCommand("INSERT INTO [Orders] ([Id], [Total]) VALUES (1, 42.50)", database, dataSource);
    }

    private static StubDbCommand MakeStoredProcCommand(string database = "MyDb", string dataSource = "localhost")
    {
        return new StubDbCommand("usp_GetOrders", database, dataSource, CommandType.StoredProcedure);
    }

    private static StubDbCommand MakeOtherCommand(string database = "MyDb", string dataSource = "localhost")
    {
        return new StubDbCommand("SET NOCOUNT ON", database, dataSource);
    }

    public void Dispose()
    {
    }

    // ─── Basic logging ─────────────────────────────────────────

    [Fact]
    public void LogCommandExecuting_And_LogCommandExecuted_Logs_request_and_response()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions());
        var command = MakeSelectCommand();

        interceptor.LogCommandExecuting(command);
        interceptor.LogCommandExecuted(command, rowsAffected: 5);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public void Logs_correct_service_and_caller_names()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(callerName: "MyApi", serviceName: "OrdersDB"));
        var command = MakeSelectCommand();

        interceptor.LogCommandExecuting(command);
        interceptor.LogCommandExecuted(command);

        var logs = GetLogsFromThisTest();
        Assert.Equal("OrdersDB", logs[0].ServiceName);
        Assert.Equal("MyApi", logs[0].CallerName);
    }

    [Fact]
    public void Does_not_log_when_no_test_info_fetcher()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        var interceptor = new SqlTrackingInterceptor(options);
        var command = MakeSelectCommand();

        interceptor.LogCommandExecuting(command);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    [Fact]
    public void NonQuery_Logs_request_and_response()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions());
        var command = MakeInsertCommand();

        interceptor.LogCommandExecuting(command);
        interceptor.LogCommandExecuted(command, rowsAffected: 1);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    [Fact]
    public void Scalar_Logs_request_and_response()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions());
        var command = MakeSelectCommand();

        interceptor.LogCommandExecuting(command);
        interceptor.LogCommandExecuted(command);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
    }

    // ─── Detailed verbosity ────────────────────────────────────

    [Fact]
    public void Detailed_Select_UsesClassifiedLabel()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Detailed));
        var command = MakeSelectCommand();

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Select", log.Method.Value?.ToString());
    }

    [Fact]
    public void Detailed_IncludesCommandTextAsContent()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Detailed));
        var command = MakeSelectCommand();

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("SELECT", log.Content);
    }

    [Fact]
    public void Detailed_BuildsCleanUriWithTableName()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Detailed));
        var command = MakeSelectCommand("OrdersDb", "sql-server.local");

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("sql://sql-server.local/OrdersDb/Users", log.Uri.ToString());
    }

    [Fact]
    public void Detailed_StoredProc_ShowsProcNameInUri()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Detailed));
        var command = MakeStoredProcCommand("OrdersDb", "sql-server.local");

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("sql://sql-server.local/OrdersDb/usp_GetOrders", log.Uri.ToString());
    }

    [Fact]
    public void Detailed_Insert_CorrectLabel()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Detailed));
        var command = MakeInsertCommand();

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Insert", log.Method.Value?.ToString());
    }

    // ─── Summarised verbosity ──────────────────────────────────

    [Fact]
    public void Summarised_UsesOperationNameOnly()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Summarised));
        var command = MakeSelectCommand();

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("Select", log.Method.Value?.ToString());
    }

    [Fact]
    public void Summarised_OmitsContent()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Summarised));
        var command = MakeSelectCommand();

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Null(log.Content);
    }

    [Fact]
    public void Summarised_UriOmitsServer()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Summarised));
        var command = MakeSelectCommand("OrdersDb", "sql-server.local");

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("sql://ordersdb/Users", log.Uri.ToString());
    }

    [Fact]
    public void Summarised_SkipsOtherOperations()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Summarised));
        var command = MakeOtherCommand();

        interceptor.LogCommandExecuting(command);

        var logs = GetLogsFromThisTest();
        Assert.Empty(logs);
    }

    [Fact]
    public void Summarised_DoesNotSkipStoredProc()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Summarised));
        var command = MakeStoredProcCommand();

        interceptor.LogCommandExecuting(command);

        var logs = GetLogsFromThisTest();
        Assert.NotEmpty(logs);
    }

    // ─── Raw verbosity ─────────────────────────────────────────

    [Fact]
    public void Raw_UsesRawSqlKeyword()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Raw));
        var command = MakeSelectCommand();

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("SELECT", log.Method.Value?.ToString());
    }

    [Fact]
    public void Raw_IncludesFullContent()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Raw));
        var command = MakeSelectCommand();

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("[dbo].[Users]", log.Content);
    }

    [Fact]
    public void Raw_DoesNotSkipOtherOperations()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Raw));
        var command = MakeOtherCommand();

        interceptor.LogCommandExecuting(command);

        var logs = GetLogsFromThisTest();
        Assert.NotEmpty(logs);
    }

    [Fact]
    public void Raw_UsesFullUri()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Raw));
        var command = MakeSelectCommand("OrdersDb", "sql-server.local");

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Equal("sql://sql-server.local/OrdersDb", log.Uri.ToString());
    }

    // ─── Response ──────────────────────────────────────────────

    [Fact]
    public void Response_CapturesRowsAffected()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions());
        var command = MakeInsertCommand();

        interceptor.LogCommandExecuting(command);
        interceptor.LogCommandExecuted(command, rowsAffected: 3);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Contains("3", log.Content);
    }

    [Fact]
    public void Response_CapturesOkStatus()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions());
        var command = MakeSelectCommand();

        interceptor.LogCommandExecuting(command);
        interceptor.LogCommandExecuted(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Response);
        Assert.Equal("OK", log.StatusCode?.Value?.ToString());
    }

    [Fact]
    public void Request_CapturesDatabaseFromConnection()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions());
        var command = MakeSelectCommand("TestDatabase", "test-server");

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("TestDatabase", log.Uri.ToString());
    }

    [Fact]
    public void Request_CapturesDataSourceFromConnection()
    {
        var interceptor = new SqlTrackingInterceptor(MakeOptions(SqlTrackingVerbosity.Detailed));
        var command = MakeSelectCommand("TestDatabase", "test-server");

        interceptor.LogCommandExecuting(command);

        var log = GetLogsFromThisTest().First(l => l.Type == RequestResponseType.Request);
        Assert.Contains("test-server", log.Uri.ToString());
    }
}
