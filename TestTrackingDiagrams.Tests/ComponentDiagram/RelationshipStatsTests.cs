using System.Diagnostics;
using System.Net;
using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.ComponentDiagram;

public class RelationshipStatsTests
{
    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static (RequestResponseLog request, RequestResponseLog response) MakeRequestResponsePair(
        string testId = "test-1",
        string testName = "My Test",
        string caller = "Caller",
        string service = "OrderService",
        string method = "GET",
        string uri = "http://sut/api/orders",
        HttpStatusCode statusCode = HttpStatusCode.OK,
        DateTimeOffset? requestTime = null,
        DateTimeOffset? responseTime = null,
        string? requestContent = null,
        string? responseContent = null)
    {
        var reqTime = requestTime ?? BaseTime;
        var resTime = responseTime ?? reqTime.AddMilliseconds(50);
        var reqId = Guid.NewGuid();

        var request = new RequestResponseLog(
            TestName: testName,
            TestId: testId,
            Method: HttpMethod.Parse(method),
            Content: requestContent,
            Uri: new Uri(uri),
            Headers: [],
            ServiceName: service,
            CallerName: caller,
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: reqId,
            TrackingIgnore: false)
        {
            Timestamp = reqTime
        };

        var response = new RequestResponseLog(
            TestName: testName,
            TestId: testId,
            Method: HttpMethod.Parse(method),
            Content: responseContent,
            Uri: new Uri(uri),
            Headers: [],
            ServiceName: service,
            CallerName: caller,
            Type: RequestResponseType.Response,
            TraceId: Guid.NewGuid(),
            RequestResponseId: reqId,
            TrackingIgnore: false,
            StatusCode: statusCode)
        {
            Timestamp = resTime
        };

        return (request, response);
    }

    private static RequestResponseLog MakeRequest(
        string testId = "test-1",
        string caller = "Caller",
        string service = "OrderService",
        string method = "GET",
        string uri = "http://sut/api/orders",
        DateTimeOffset? timestamp = null)
    {
        return new RequestResponseLog(
            TestName: "Test",
            TestId: testId,
            Method: HttpMethod.Parse(method),
            Content: null,
            Uri: new Uri(uri),
            Headers: [],
            ServiceName: service,
            CallerName: caller,
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false)
        {
            Timestamp = timestamp ?? BaseTime
        };
    }

    // ═══════════════════════════════════════════════════════════
    // 1.1 ComputeRelationshipStats — Basic percentiles
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ComputeRelationshipStats_empty_logs_returns_empty()
    {
        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats([], []);
        Assert.Empty(result);
    }

    [Fact]
    public void ComputeRelationshipStats_no_matching_responses_returns_empty()
    {
        var logs = new[] { MakeRequest() };
        var rels = ComponentDiagramGenerator.ExtractRelationships(logs);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logs);

        Assert.Empty(result);
    }

    [Fact]
    public void ComputeRelationshipStats_single_call_computes_stats()
    {
        var (req, res) = MakeRequestResponsePair(
            requestTime: BaseTime,
            responseTime: BaseTime.AddMilliseconds(100));

        var logs = new[] { req, res };
        var rels = ComponentDiagramGenerator.ExtractRelationships(logs);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logs);

        Assert.Single(result);
        var stats = result.Values.First();
        Assert.Equal(1, stats.CallCount);
        Assert.Equal(1, stats.TestCount);
        Assert.Equal(100, stats.MeanMs, 1);
        Assert.Equal(100, stats.MedianMs, 1);
        Assert.Equal(100, stats.P95Ms, 1);
        Assert.Equal(100, stats.P99Ms, 1);
        Assert.Equal(100, stats.MinMs, 1);
        Assert.Equal(100, stats.MaxMs, 1);
    }

    [Fact]
    public void ComputeRelationshipStats_multiple_calls_computes_percentiles()
    {
        // Create 100 calls with durations 1ms, 2ms, ..., 100ms
        var logs = new List<RequestResponseLog>();
        for (int i = 1; i <= 100; i++)
        {
            var (req, res) = MakeRequestResponsePair(
                testId: $"test-{i}",
                requestTime: BaseTime.AddSeconds(i),
                responseTime: BaseTime.AddSeconds(i).AddMilliseconds(i));
            logs.Add(req);
            logs.Add(res);
        }

        var logsArray = logs.ToArray();
        var rels = ComponentDiagramGenerator.ExtractRelationships(logsArray);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logsArray);

        Assert.Single(result);
        var stats = result.Values.First();
        Assert.Equal(100, stats.CallCount);
        Assert.Equal(100, stats.TestCount);
        Assert.Equal(50.5, stats.MeanMs, 1);   // mean of 1..100
        Assert.Equal(50.5, stats.MedianMs, 1);  // P50 interpolated
        Assert.Equal(95, stats.P95Ms, 1);       // P95 ≈ 95
        Assert.Equal(99, stats.P99Ms, 1);       // P99 ≈ 99
        Assert.Equal(1, stats.MinMs, 1);
        Assert.Equal(100, stats.MaxMs, 1);
    }

    [Fact]
    public void ComputeRelationshipStats_groups_by_caller_service()
    {
        var (req1, res1) = MakeRequestResponsePair(caller: "A", service: "B",
            requestTime: BaseTime, responseTime: BaseTime.AddMilliseconds(10));
        var (req2, res2) = MakeRequestResponsePair(caller: "A", service: "C",
            requestTime: BaseTime, responseTime: BaseTime.AddMilliseconds(20));

        var logs = new[] { req1, res1, req2, res2 };
        var rels = ComponentDiagramGenerator.ExtractRelationships(logs);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logs);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ComputeRelationshipStats_matches_by_RequestResponseId()
    {
        // Ensure correct pairing — not pairing request with wrong response
        var reqId1 = Guid.NewGuid();
        var reqId2 = Guid.NewGuid();

        var req1 = new RequestResponseLog("T1", "t1", HttpMethod.Get, null, new Uri("http://sut/api"),
            [], "Svc", "Call", RequestResponseType.Request, Guid.NewGuid(), reqId1, false) { Timestamp = BaseTime };
        var res1 = new RequestResponseLog("T1", "t1", HttpMethod.Get, null, new Uri("http://sut/api"),
            [], "Svc", "Call", RequestResponseType.Response, Guid.NewGuid(), reqId1, false, HttpStatusCode.OK) { Timestamp = BaseTime.AddMilliseconds(50) };

        var req2 = new RequestResponseLog("T2", "t2", HttpMethod.Get, null, new Uri("http://sut/api"),
            [], "Svc", "Call", RequestResponseType.Request, Guid.NewGuid(), reqId2, false) { Timestamp = BaseTime.AddSeconds(1) };
        var res2 = new RequestResponseLog("T2", "t2", HttpMethod.Get, null, new Uri("http://sut/api"),
            [], "Svc", "Call", RequestResponseType.Response, Guid.NewGuid(), reqId2, false, HttpStatusCode.OK) { Timestamp = BaseTime.AddSeconds(1).AddMilliseconds(200) };

        var logs = new[] { req1, res1, req2, res2 };
        var rels = ComponentDiagramGenerator.ExtractRelationships(logs);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logs);

        Assert.Single(result);
        var stats = result.Values.First();
        // Mean of 50ms and 200ms = 125ms
        Assert.Equal(125, stats.MeanMs, 1);
        Assert.Equal(50, stats.MinMs, 1);
        Assert.Equal(200, stats.MaxMs, 1);
    }

    [Fact]
    public void ComputeRelationshipStats_scales_to_1500_tests()
    {
        var logs = new List<RequestResponseLog>();
        var random = new Random(42);

        for (int i = 0; i < 1500; i++)
        {
            var duration = random.Next(1, 500);
            var (req, res) = MakeRequestResponsePair(
                testId: $"test-{i}",
                requestTime: BaseTime.AddSeconds(i),
                responseTime: BaseTime.AddSeconds(i).AddMilliseconds(duration));
            logs.Add(req);
            logs.Add(res);
        }

        var logsArray = logs.ToArray();
        var rels = ComponentDiagramGenerator.ExtractRelationships(logsArray);

        var sw = Stopwatch.StartNew();
        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logsArray);
        sw.Stop();

        Assert.Single(result);
        var stats = result.Values.First();
        Assert.Equal(1500, stats.CallCount);
        Assert.True(sw.ElapsedMilliseconds < 500, $"Took {sw.ElapsedMilliseconds}ms, expected <500ms");
    }

    [Fact]
    public void ComputeRelationshipStats_key_uses_iflow_rel_prefix()
    {
        var (req, res) = MakeRequestResponsePair(caller: "API", service: "DB");
        var logs = new[] { req, res };
        var rels = ComponentDiagramGenerator.ExtractRelationships(logs);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logs);

        var key = result.Keys.Single();
        Assert.StartsWith("iflow-rel-", key);
        Assert.Contains("API", key);
        Assert.Contains("DB", key);
    }

    // ═══════════════════════════════════════════════════════════
    // 1.2 Error rate + status code distribution (Features #1, #2)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ComputeRelationshipStats_error_rate_all_success()
    {
        var (req, res) = MakeRequestResponsePair(statusCode: HttpStatusCode.OK);
        var logs = new[] { req, res };
        var rels = ComponentDiagramGenerator.ExtractRelationships(logs);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logs);

        Assert.Equal(0.0, result.Values.First().ErrorRate, 2);
    }

    [Fact]
    public void ComputeRelationshipStats_error_rate_with_failures()
    {
        var logs = new List<RequestResponseLog>();
        for (int i = 0; i < 10; i++)
        {
            var status = i < 7 ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
            var (req, res) = MakeRequestResponsePair(
                testId: $"test-{i}",
                statusCode: status,
                requestTime: BaseTime.AddSeconds(i),
                responseTime: BaseTime.AddSeconds(i).AddMilliseconds(10));
            logs.Add(req);
            logs.Add(res);
        }

        var logsArray = logs.ToArray();
        var rels = ComponentDiagramGenerator.ExtractRelationships(logsArray);
        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logsArray);

        Assert.Equal(0.3, result.Values.First().ErrorRate, 2); // 3 out of 10
    }

    [Fact]
    public void ComputeRelationshipStats_status_code_distribution()
    {
        var logs = new List<RequestResponseLog>();
        var statuses = new[] { HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK,
                               HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError };

        for (int i = 0; i < statuses.Length; i++)
        {
            var (req, res) = MakeRequestResponsePair(
                testId: $"test-{i}",
                statusCode: statuses[i],
                requestTime: BaseTime.AddSeconds(i),
                responseTime: BaseTime.AddSeconds(i).AddMilliseconds(10));
            logs.Add(req);
            logs.Add(res);
        }

        var logsArray = logs.ToArray();
        var rels = ComponentDiagramGenerator.ExtractRelationships(logsArray);
        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logsArray);
        var dist = result.Values.First().StatusCodeDistribution;

        Assert.Equal(3, dist[HttpStatusCode.OK]);
        Assert.Equal(1, dist[HttpStatusCode.BadRequest]);
        Assert.Equal(1, dist[HttpStatusCode.InternalServerError]);
    }

    // ═══════════════════════════════════════════════════════════
    // 1.3 Payload size stats (Feature #4)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ComputeRelationshipStats_payload_sizes_null_when_no_content()
    {
        var (req, res) = MakeRequestResponsePair();
        var logs = new[] { req, res };
        var rels = ComponentDiagramGenerator.ExtractRelationships(logs);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logs);

        Assert.Null(result.Values.First().PayloadSizes);
    }

    [Fact]
    public void ComputeRelationshipStats_payload_sizes_computed()
    {
        var (req, res) = MakeRequestResponsePair(
            requestContent: new string('x', 1000),
            responseContent: new string('y', 2000));
        var logs = new[] { req, res };
        var rels = ComponentDiagramGenerator.ExtractRelationships(logs);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logs);

        var sizes = result.Values.First().PayloadSizes;
        Assert.NotNull(sizes);
        Assert.Equal(1000, sizes.RequestMeanBytes, 1);
        Assert.Equal(2000, sizes.ResponseMeanBytes, 1);
    }

    // ═══════════════════════════════════════════════════════════
    // 1.4 Endpoint breakdown (Feature #8)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ComputeRelationshipStats_endpoint_breakdown()
    {
        var (req1, res1) = MakeRequestResponsePair(
            method: "GET", uri: "http://sut/api/orders",
            requestTime: BaseTime, responseTime: BaseTime.AddMilliseconds(10));
        var (req2, res2) = MakeRequestResponsePair(
            testId: "test-2",
            method: "POST", uri: "http://sut/api/orders",
            requestTime: BaseTime.AddSeconds(1), responseTime: BaseTime.AddSeconds(1).AddMilliseconds(50));
        var (req3, res3) = MakeRequestResponsePair(
            testId: "test-3",
            method: "GET", uri: "http://sut/api/orders",
            requestTime: BaseTime.AddSeconds(2), responseTime: BaseTime.AddSeconds(2).AddMilliseconds(20));

        var logs = new[] { req1, res1, req2, res2, req3, res3 };
        var rels = ComponentDiagramGenerator.ExtractRelationships(logs);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logs);
        var endpoints = result.Values.First().EndpointBreakdown;

        Assert.Equal(2, endpoints.Length);
        var getEndpoint = endpoints.Single(e => e.Method == "GET");
        Assert.Equal(2, getEndpoint.CallCount);
        Assert.Equal(15, getEndpoint.MeanMs, 1); // (10+20)/2

        var postEndpoint = endpoints.Single(e => e.Method == "POST");
        Assert.Equal(1, postEndpoint.CallCount);
        Assert.Equal(50, postEndpoint.MeanMs, 1);
    }

    // ═══════════════════════════════════════════════════════════
    // 1.5 Call ordering within tests (Feature #5)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BuildCallOrdering_returns_numbered_sequence_per_test()
    {
        var logs = new[]
        {
            MakeRequest(testId: "t1", caller: "Tests", service: "API", method: "POST",
                uri: "http://sut/api/orders", timestamp: BaseTime),
            MakeRequest(testId: "t1", caller: "API", service: "DB", method: "GET",
                uri: "http://db/query", timestamp: BaseTime.AddMilliseconds(10)),
            MakeRequest(testId: "t1", caller: "API", service: "Cache", method: "GET",
                uri: "http://cache/get", timestamp: BaseTime.AddMilliseconds(20))
        };

        var result = ComponentFlowSegmentBuilder.BuildCallOrdering(logs);

        Assert.Single(result);
        Assert.Equal("t1", result[0].TestId);
        Assert.Equal(3, result[0].Entries.Length);
        Assert.Equal(1, result[0].Entries[0].Position);
        Assert.Equal("API", result[0].Entries[0].Service);
        Assert.Equal(2, result[0].Entries[1].Position);
        Assert.Equal("DB", result[0].Entries[1].Service);
        Assert.Equal(3, result[0].Entries[2].Position);
        Assert.Equal("Cache", result[0].Entries[2].Service);
    }

    [Fact]
    public void BuildCallOrdering_groups_by_test()
    {
        var logs = new[]
        {
            MakeRequest(testId: "t1", caller: "A", service: "B", timestamp: BaseTime),
            MakeRequest(testId: "t2", caller: "A", service: "C", timestamp: BaseTime)
        };

        var result = ComponentFlowSegmentBuilder.BuildCallOrdering(logs);

        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void BuildCallOrdering_empty_logs_returns_empty()
    {
        var result = ComponentFlowSegmentBuilder.BuildCallOrdering([]);
        Assert.Empty(result);
    }

    // ═══════════════════════════════════════════════════════════
    // 1.6 Concurrency detection (Feature #6)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ComputeRelationshipStats_detects_concurrent_calls()
    {
        // Two calls from API that overlap in time within the same test
        var reqId1 = Guid.NewGuid();
        var reqId2 = Guid.NewGuid();

        var logs = new RequestResponseLog[]
        {
            new("T", "t1", HttpMethod.Get, null, new Uri("http://sut/db"), [], "DB", "API",
                RequestResponseType.Request, Guid.NewGuid(), reqId1, false) { Timestamp = BaseTime },
            new("T", "t1", HttpMethod.Get, null, new Uri("http://sut/cache"), [], "Cache", "API",
                RequestResponseType.Request, Guid.NewGuid(), reqId2, false) { Timestamp = BaseTime.AddMilliseconds(5) },
            new("T", "t1", HttpMethod.Get, null, new Uri("http://sut/db"), [], "DB", "API",
                RequestResponseType.Response, Guid.NewGuid(), reqId1, false, HttpStatusCode.OK) { Timestamp = BaseTime.AddMilliseconds(50) },
            new("T", "t1", HttpMethod.Get, null, new Uri("http://sut/cache"), [], "Cache", "API",
                RequestResponseType.Response, Guid.NewGuid(), reqId2, false, HttpStatusCode.OK) { Timestamp = BaseTime.AddMilliseconds(40) }
        };

        var rels = ComponentDiagramGenerator.ExtractRelationships(logs);
        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logs);

        // Both relationships should have concurrency info since they overlap
        var dbStats = result.Values.First(s => result.Keys.First(k => result[k] == s).Contains("DB"));
        Assert.NotNull(dbStats.Concurrency);
        Assert.True(dbStats.Concurrency.ConcurrencyPercentage > 0);
    }

    // ═══════════════════════════════════════════════════════════
    // 1.7 Low-coverage warnings (Feature #7)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ComputeRelationshipStats_low_coverage_when_few_tests()
    {
        var (req, res) = MakeRequestResponsePair(testId: "t1");
        var logs = new[] { req, res };
        var rels = ComponentDiagramGenerator.ExtractRelationships(logs);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logs, lowCoverageThreshold: 3);

        Assert.True(result.Values.First().IsLowCoverage);
    }

    [Fact]
    public void ComputeRelationshipStats_not_low_coverage_when_enough_tests()
    {
        var logs = new List<RequestResponseLog>();
        for (int i = 0; i < 5; i++)
        {
            var (req, res) = MakeRequestResponsePair(
                testId: $"test-{i}",
                requestTime: BaseTime.AddSeconds(i),
                responseTime: BaseTime.AddSeconds(i).AddMilliseconds(10));
            logs.Add(req);
            logs.Add(res);
        }

        var logsArray = logs.ToArray();
        var rels = ComponentDiagramGenerator.ExtractRelationships(logsArray);

        var result = ComponentFlowSegmentBuilder.ComputeRelationshipStats(rels, logsArray, lowCoverageThreshold: 3);

        Assert.False(result.Values.First().IsLowCoverage);
    }
}
