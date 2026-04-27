using Google.Cloud.Spanner.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Interceptors;
using TestTrackingDiagrams.Extensions.Spanner;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Spanner;

public class SpannerTrackingInterceptorTests
{
    private readonly string _testId = Guid.NewGuid().ToString();

    private RequestResponseLog[] GetLogs() =>
        RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == _testId)
            .ToArray();

    private SpannerTrackingOptions MakeOptions(
        SpannerTrackingVerbosity verbosity = SpannerTrackingVerbosity.Detailed) => new()
    {
        ServiceName = "Spanner",
        CallingServiceName = "TestCaller",
        Verbosity = verbosity,
        CurrentTestInfoFetcher = () => ("Interceptor Test", _testId),
    };

    private SpannerTrackingInterceptor CreateInterceptor(SpannerTrackingOptions? options = null) =>
        new(options ?? MakeOptions());

    // ─── Helpers for faking gRPC calls ──────────────────────

    private static Method<TRequest, TResponse> MakeMethod<TRequest, TResponse>(
        string serviceName, string methodName, MethodType type = MethodType.Unary)
        where TRequest : class where TResponse : class
    {
        return new Method<TRequest, TResponse>(
            type, serviceName, methodName,
            Marshallers.Create<TRequest>(
                msg => ((IMessage)msg).ToByteArray(),
                bytes => default!),
            Marshallers.Create<TResponse>(
                msg => ((IMessage)msg).ToByteArray(),
                bytes => default!));
    }

    private static AsyncUnaryCall<TResponse> FakeUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    // ─── AsyncUnaryCall Tests ────────────────────────────────

    [Fact]
    public void AsyncUnaryCall_ExecuteSql_Logs_request_and_response()
    {
        var interceptor = CreateInterceptor();
        var request = new ExecuteSqlRequest
        {
            Sql = "SELECT * FROM Users WHERE Id = @id",
            Session = "projects/p/instances/i/databases/d/sessions/s"
        };
        var response = new ResultSet();
        var method = MakeMethod<ExecuteSqlRequest, ResultSet>(
            "google.spanner.v1.Spanner", "ExecuteSql");
        var context = new ClientInterceptorContext<ExecuteSqlRequest, ResultSet>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context, (req, ctx) => FakeUnaryCall(response));

        // Wait for the response task
        var logs = GetLogs();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public void AsyncUnaryCall_ExecuteSql_Extracts_sql_text()
    {
        var interceptor = CreateInterceptor();
        var request = new ExecuteSqlRequest
        {
            Sql = "SELECT Name FROM Users",
            Session = "projects/p/instances/i/databases/d/sessions/s"
        };
        var method = MakeMethod<ExecuteSqlRequest, ResultSet>(
            "google.spanner.v1.Spanner", "ExecuteSql");
        var context = new ClientInterceptorContext<ExecuteSqlRequest, ResultSet>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new ResultSet()));

        var log = GetLogs().First();
        Assert.Contains("SELECT Name FROM Users", log.Content);
    }

    [Fact]
    public void AsyncUnaryCall_Read_Extracts_table_name()
    {
        var interceptor = CreateInterceptor();
        var request = new ReadRequest
        {
            Table = "Orders",
            Session = "projects/p/instances/i/databases/d/sessions/s"
        };
        var method = MakeMethod<ReadRequest, ResultSet>(
            "google.spanner.v1.Spanner", "Read");
        var context = new ClientInterceptorContext<ReadRequest, ResultSet>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new ResultSet()));

        var log = GetLogs().First();
        Assert.Contains("Orders", log.Method.Value?.ToString());
    }

    [Fact]
    public void AsyncUnaryCall_Commit_Extracts_mutation_table_names()
    {
        var interceptor = CreateInterceptor();
        var request = new CommitRequest
        {
            Session = "projects/p/instances/i/databases/d/sessions/s"
        };
        request.Mutations.Add(new Mutation
        {
            Insert = new Mutation.Types.Write { Table = "Feedback" }
        });
        var method = MakeMethod<CommitRequest, CommitResponse>(
            "google.spanner.v1.Spanner", "Commit");
        var context = new ClientInterceptorContext<CommitRequest, CommitResponse>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new CommitResponse()));

        var log = GetLogs().First();
        Assert.Contains("Feedback", log.Content ?? log.Method.Value?.ToString());
    }

    [Fact]
    public void AsyncUnaryCall_Commit_Multiple_mutations_extracts_all_tables()
    {
        var interceptor = CreateInterceptor();
        var request = new CommitRequest
        {
            Session = "projects/p/instances/i/databases/d/sessions/s"
        };
        request.Mutations.Add(new Mutation
        {
            Insert = new Mutation.Types.Write { Table = "Orders" }
        });
        request.Mutations.Add(new Mutation
        {
            InsertOrUpdate = new Mutation.Types.Write { Table = "Preferences" }
        });
        var method = MakeMethod<CommitRequest, CommitResponse>(
            "google.spanner.v1.Spanner", "Commit");
        var context = new ClientInterceptorContext<CommitRequest, CommitResponse>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new CommitResponse()));

        var log = GetLogs().First();
        var content = log.Content ?? log.Method.Value?.ToString();
        Assert.Contains("Orders", content);
        Assert.Contains("Preferences", content);
    }

    [Fact]
    public void AsyncUnaryCall_ExcludedOperation_No_logs()
    {
        var options = MakeOptions();
        options.ExcludedOperations = [SpannerOperation.CreateSession];
        var interceptor = CreateInterceptor(options);
        var request = new CreateSessionRequest
        {
            Database = "projects/p/instances/i/databases/d"
        };
        var method = MakeMethod<CreateSessionRequest, Session>(
            "google.spanner.v1.Spanner", "CreateSession");
        var context = new ClientInterceptorContext<CreateSessionRequest, Session>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new Session()));

        Assert.Empty(GetLogs());
    }

    [Fact]
    public void AsyncUnaryCall_NoTestInfo_Still_forwards_call()
    {
        var options = MakeOptions();
        options.CurrentTestInfoFetcher = null;
        var interceptor = CreateInterceptor(options);
        var request = new ExecuteSqlRequest { Sql = "SELECT 1" };
        var method = MakeMethod<ExecuteSqlRequest, ResultSet>(
            "google.spanner.v1.Spanner", "ExecuteSql");
        var context = new ClientInterceptorContext<ExecuteSqlRequest, ResultSet>(
            method, null, new CallOptions());
        var called = false;

        interceptor.AsyncUnaryCall(request, context, (req, ctx) =>
        {
            called = true;
            return FakeUnaryCall(new ResultSet());
        });

        Assert.True(called);
        Assert.Empty(GetLogs());
    }

    [Fact]
    public void AsyncUnaryCall_Uses_Database_dependency_category()
    {
        var interceptor = CreateInterceptor();
        var request = new ExecuteSqlRequest { Sql = "SELECT 1" };
        var method = MakeMethod<ExecuteSqlRequest, ResultSet>(
            "google.spanner.v1.Spanner", "ExecuteSql");
        var context = new ClientInterceptorContext<ExecuteSqlRequest, ResultSet>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new ResultSet()));

        var log = GetLogs().First();
        Assert.Equal("Database", log.DependencyCategory);
    }

    [Fact]
    public void AsyncUnaryCall_Summarised_Omits_content()
    {
        var interceptor = CreateInterceptor(MakeOptions(SpannerTrackingVerbosity.Summarised));
        var request = new ExecuteSqlRequest { Sql = "SELECT * FROM Users" };
        var method = MakeMethod<ExecuteSqlRequest, ResultSet>(
            "google.spanner.v1.Spanner", "ExecuteSql");
        var context = new ClientInterceptorContext<ExecuteSqlRequest, ResultSet>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new ResultSet()));

        var log = GetLogs().First();
        Assert.Null(log.Content);
        Assert.Equal("SELECT", log.Method.Value?.ToString());
    }

    [Fact]
    public void AsyncUnaryCall_Pairs_request_and_response_traceIds()
    {
        var interceptor = CreateInterceptor();
        var request = new ExecuteSqlRequest
        {
            Sql = "SELECT 1",
            Session = "projects/p/instances/i/databases/d/sessions/s"
        };
        var method = MakeMethod<ExecuteSqlRequest, ResultSet>(
            "google.spanner.v1.Spanner", "ExecuteSql");
        var context = new ClientInterceptorContext<ExecuteSqlRequest, ResultSet>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new ResultSet()));

        var logs = GetLogs();
        Assert.Equal(2, logs.Length);
        Assert.Equal(logs[0].TraceId, logs[1].TraceId);
        Assert.Equal(logs[0].RequestResponseId, logs[1].RequestResponseId);
    }

    // ─── ITrackingComponent ─────────────────────────────────

    [Fact]
    public void Implements_ITrackingComponent()
    {
        var interceptor = CreateInterceptor();
        Assert.IsAssignableFrom<ITrackingComponent>(interceptor);
    }

    [Fact]
    public void WasInvoked_false_before_calls()
    {
        var interceptor = CreateInterceptor();
        Assert.False(interceptor.WasInvoked);
    }

    [Fact]
    public void WasInvoked_true_after_call()
    {
        var interceptor = CreateInterceptor();
        var request = new ExecuteSqlRequest { Sql = "SELECT 1" };
        var method = MakeMethod<ExecuteSqlRequest, ResultSet>(
            "google.spanner.v1.Spanner", "ExecuteSql");
        var context = new ClientInterceptorContext<ExecuteSqlRequest, ResultSet>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new ResultSet()));

        Assert.True(interceptor.WasInvoked);
    }

    [Fact]
    public void InvocationCount_increments()
    {
        var interceptor = CreateInterceptor();
        var method = MakeMethod<ExecuteSqlRequest, ResultSet>(
            "google.spanner.v1.Spanner", "ExecuteSql");
        var context = new ClientInterceptorContext<ExecuteSqlRequest, ResultSet>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(new ExecuteSqlRequest { Sql = "s1" }, context,
            (req, ctx) => FakeUnaryCall(new ResultSet()));
        interceptor.AsyncUnaryCall(new ExecuteSqlRequest { Sql = "s2" }, context,
            (req, ctx) => FakeUnaryCall(new ResultSet()));

        Assert.Equal(2, interceptor.InvocationCount);
    }

    [Fact]
    public void ComponentName_includes_service_name()
    {
        var interceptor = CreateInterceptor();
        Assert.Contains("Spanner", interceptor.ComponentName);
    }

    // ─── URI ────────────────────────────────────────────────

    [Fact]
    public void AsyncUnaryCall_URI_uses_spanner_scheme()
    {
        var interceptor = CreateInterceptor();
        var request = new ExecuteSqlRequest
        {
            Sql = "SELECT * FROM Users",
            Session = "projects/p/instances/i/databases/d/sessions/s"
        };
        var method = MakeMethod<ExecuteSqlRequest, ResultSet>(
            "google.spanner.v1.Spanner", "ExecuteSql");
        var context = new ClientInterceptorContext<ExecuteSqlRequest, ResultSet>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new ResultSet()));

        var log = GetLogs().First();
        Assert.StartsWith("spanner://", log.Uri.ToString());
    }

    // ─── Mutation type extraction ──────────────────────────

    [Fact]
    public void AsyncUnaryCall_Commit_InsertOrUpdate_classified_correctly()
    {
        var interceptor = CreateInterceptor();
        var request = new CommitRequest
        {
            Session = "projects/p/instances/i/databases/d/sessions/s"
        };
        request.Mutations.Add(new Mutation
        {
            InsertOrUpdate = new Mutation.Types.Write { Table = "Preferences" }
        });
        var method = MakeMethod<CommitRequest, CommitResponse>(
            "google.spanner.v1.Spanner", "Commit");
        var context = new ClientInterceptorContext<CommitRequest, CommitResponse>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new CommitResponse()));

        var log = GetLogs().First();
        Assert.Contains("Preferences", log.Content ?? log.Method.Value?.ToString());
    }

    [Fact]
    public void AsyncUnaryCall_Commit_Delete_mutation_classified()
    {
        var interceptor = CreateInterceptor();
        var request = new CommitRequest
        {
            Session = "projects/p/instances/i/databases/d/sessions/s"
        };
        request.Mutations.Add(new Mutation
        {
            Delete = new Mutation.Types.Delete { Table = "Feedback" }
        });
        var method = MakeMethod<CommitRequest, CommitResponse>(
            "google.spanner.v1.Spanner", "Commit");
        var context = new ClientInterceptorContext<CommitRequest, CommitResponse>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new CommitResponse()));

        var log = GetLogs().First();
        Assert.Contains("Feedback", log.Content ?? log.Method.Value?.ToString());
    }

    // ─── BatchDml extraction ───────────────────────────────

    [Fact]
    public void AsyncUnaryCall_ExecuteBatchDml_extracts_sql_statements()
    {
        var interceptor = CreateInterceptor();
        var request = new ExecuteBatchDmlRequest
        {
            Session = "projects/p/instances/i/databases/d/sessions/s"
        };
        request.Statements.Add(new ExecuteBatchDmlRequest.Types.Statement { Sql = "INSERT INTO Orders VALUES (@id)" });
        request.Statements.Add(new ExecuteBatchDmlRequest.Types.Statement { Sql = "UPDATE Stock SET Qty = Qty - 1" });
        var method = MakeMethod<ExecuteBatchDmlRequest, ExecuteBatchDmlResponse>(
            "google.spanner.v1.Spanner", "ExecuteBatchDml");
        var context = new ClientInterceptorContext<ExecuteBatchDmlRequest, ExecuteBatchDmlResponse>(
            method, null, new CallOptions());

        interceptor.AsyncUnaryCall(request, context,
            (req, ctx) => FakeUnaryCall(new ExecuteBatchDmlResponse()));

        var log = GetLogs().First();
        Assert.Contains("INSERT INTO Orders", log.Content!);
        Assert.Contains("UPDATE Stock", log.Content!);
    }
}
