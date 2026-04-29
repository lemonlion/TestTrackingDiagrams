using Google.Cloud.Spanner.V1;
using Google.Protobuf;
using Grpc.Core;
using TestTrackingDiagrams.Extensions.Spanner;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Spanner;

public class SpannerTrackerServerObserverTests
{
    private readonly string _testId = Guid.NewGuid().ToString();

    private RequestResponseLog[] GetLogs() =>
        RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.TestId == _testId)
            .ToArray();

    private SpannerTrackingOptions MakeOptions() => new()
    {
        ServiceName = "Spanner",
        CallerName = "TestCaller",
        Verbosity = SpannerTrackingVerbosity.Detailed,
        CurrentTestInfoFetcher = () => ("Observer Test", _testId),
    };

    // ─── CreateServerObservers ──────────────────────────────

    [Fact]
    public void CreateServerObservers_Returns_both_delegates()
    {
        var tracker = new SpannerTracker(MakeOptions());

        var (onRequest, onResponse) = SpannerTracker.CreateServerObservers(tracker);

        Assert.NotNull(onRequest);
        Assert.NotNull(onResponse);
    }

    [Fact]
    public void OnRequest_Logs_request_entry()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var (onRequest, _) = SpannerTracker.CreateServerObservers(tracker);

        var request = new ExecuteSqlRequest { Sql = "SELECT * FROM Users" };
        onRequest("ExecuteSql", request, DateTimeOffset.UtcNow);

        var logs = GetLogs();
        Assert.Single(logs);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
    }

    [Fact]
    public void OnResponse_Logs_response_entry()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var (onRequest, onResponse) = SpannerTracker.CreateServerObservers(tracker);

        var request = new ExecuteSqlRequest { Sql = "SELECT * FROM Users" };
        onRequest("ExecuteSql", request, DateTimeOffset.UtcNow);
        onResponse("ExecuteSql", request, new ResultSet(), TimeSpan.FromMilliseconds(5), StatusCode.OK, DateTimeOffset.UtcNow);

        var logs = GetLogs();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public void OnRequest_Extracts_sql_from_ExecuteSqlRequest()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var (onRequest, _) = SpannerTracker.CreateServerObservers(tracker);

        var request = new ExecuteSqlRequest { Sql = "SELECT Name FROM Users" };
        onRequest("ExecuteSql", request, DateTimeOffset.UtcNow);

        var log = GetLogs().First();
        Assert.Contains("SELECT Name FROM Users", log.Content);
    }

    [Fact]
    public void OnRequest_Extracts_table_from_ReadRequest()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var (onRequest, _) = SpannerTracker.CreateServerObservers(tracker);

        var request = new ReadRequest { Table = "Orders" };
        onRequest("Read", request, DateTimeOffset.UtcNow);

        var log = GetLogs().First();
        Assert.Contains("Orders", log.Method.Value?.ToString());
    }

    [Fact]
    public void OnRequest_Extracts_mutation_from_CommitRequest()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var (onRequest, _) = SpannerTracker.CreateServerObservers(tracker);

        var request = new CommitRequest();
        request.Mutations.Add(new Mutation
        {
            Insert = new Mutation.Types.Write { Table = "Feedback" }
        });
        onRequest("Commit", request, DateTimeOffset.UtcNow);

        var log = GetLogs().First();
        Assert.Contains("Feedback", log.Content ?? log.Method.Value?.ToString());
    }

    [Fact]
    public void OnRequest_Non_protobuf_message_still_logs()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var (onRequest, _) = SpannerTracker.CreateServerObservers(tracker);

        // Use a generic message that's not a recognized Spanner type
        var request = new BeginTransactionRequest();
        onRequest("BeginTransaction", request, DateTimeOffset.UtcNow);

        var logs = GetLogs();
        Assert.Single(logs);
    }

    [Fact]
    public void OnRequest_Uses_Database_dependency_category()
    {
        var tracker = new SpannerTracker(MakeOptions());
        var (onRequest, _) = SpannerTracker.CreateServerObservers(tracker);

        onRequest("ExecuteSql", new ExecuteSqlRequest { Sql = "SELECT 1" }, DateTimeOffset.UtcNow);

        var log = GetLogs().First();
        Assert.Equal("Spanner", log.DependencyCategory);
    }

    [Fact]
    public void ExcludedOperations_are_respected()
    {
        var options = MakeOptions();
        options.ExcludedOperations = [SpannerOperation.CreateSession];
        var tracker = new SpannerTracker(options);
        var (onRequest, _) = SpannerTracker.CreateServerObservers(tracker);

        onRequest("CreateSession", new CreateSessionRequest(), DateTimeOffset.UtcNow);

        Assert.Empty(GetLogs());
    }
}
