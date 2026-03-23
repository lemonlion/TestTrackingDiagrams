using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Constants;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

public class MessageTrackerTests
{
    private readonly int _logCountBefore = RequestResponseLogger.RequestAndResponseLogs.Length;

    private RequestResponseLog[] GetLogsFromThisTest()
    {
        return RequestResponseLogger.RequestAndResponseLogs.Skip(_logCountBefore).ToArray();
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(
        string testName = "My Test",
        string testId = "test-id-1",
        string traceId = "7c9e6679-7425-40de-944b-e07fc1f90ae7")
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[TestTrackingHttpHeaders.CurrentTestNameHeader] = testName;
        context.Request.Headers[TestTrackingHttpHeaders.CurrentTestIdHeader] = testId;
        context.Request.Headers[TestTrackingHttpHeaders.TraceIdHeader] = traceId;

        return new HttpContextAccessor { HttpContext = context };
    }

    private static MessageTracker CreateTracker(
        IHttpContextAccessor? accessor = null,
        string callingServiceName = "MyService")
    {
        return new MessageTracker(accessor ?? CreateHttpContextAccessor(), callingServiceName);
    }

    // ─── LogRequest ─────────────────────────────────────────────

    [Fact]
    public void LogRequest_logs_a_request_entry()
    {
        var tracker = CreateTracker();

        tracker.LogRequest("Kafka", "OrderService", new Uri("kafka://orders-topic"), new { Id = 1 });

        var logs = GetLogsFromThisTest();
        Assert.Single(logs);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
    }

    [Fact]
    public void LogRequest_returns_a_non_empty_correlation_id()
    {
        var tracker = CreateTracker();

        var id = tracker.LogRequest("Kafka", "OrderService", new Uri("kafka://orders-topic"), new { Id = 1 });

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public void LogRequest_sets_protocol_as_method()
    {
        var tracker = CreateTracker();

        tracker.LogRequest("EventGrid", "NotificationService", new Uri("eventgrid://events"), new { Msg = "hi" });

        var log = GetLogsFromThisTest().Single();
        Assert.Equal("EventGrid", log.Method.Value);
    }

    [Fact]
    public void LogRequest_sets_destination_as_service_name()
    {
        var tracker = CreateTracker();

        tracker.LogRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { });

        var log = GetLogsFromThisTest().Single();
        Assert.Equal("OrderService", log.ServiceName);
    }

    [Fact]
    public void LogRequest_sets_calling_service_name()
    {
        var tracker = CreateTracker(callingServiceName: "PublisherApp");

        tracker.LogRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { });

        var log = GetLogsFromThisTest().Single();
        Assert.Equal("PublisherApp", log.CallerName);
    }

    [Fact]
    public void LogRequest_serialises_payload_as_content()
    {
        var tracker = CreateTracker();

        tracker.LogRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { Name = "Widget" });

        var log = GetLogsFromThisTest().Single();
        Assert.Contains("Widget", log.Content);
    }

    [Fact]
    public void LogRequest_sets_uri()
    {
        var uri = new Uri("kafka://my-topic");
        var tracker = CreateTracker();

        tracker.LogRequest("Kafka", "OrderService", uri, new { });

        var log = GetLogsFromThisTest().Single();
        Assert.Equal(uri, log.Uri);
    }

    [Fact]
    public void LogRequest_sets_meta_type_to_event()
    {
        var tracker = CreateTracker();

        tracker.LogRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { });

        var log = GetLogsFromThisTest().Single();
        Assert.Equal(RequestResponseMetaType.Event, log.MetaType);
    }

    [Fact]
    public void LogRequest_reads_test_info_from_http_context()
    {
        var accessor = CreateHttpContextAccessor("SpecificTest", "id-42", "7c9e6679-7425-40de-944b-e07fc1f90ae7");
        var tracker = CreateTracker(accessor);

        tracker.LogRequest("SNS", "NotifySvc", new Uri("sns://topic"), new { });

        var log = GetLogsFromThisTest().Single();
        Assert.Equal("SpecificTest", log.TestName);
        Assert.Equal("id-42", log.TestId);
        Assert.Equal(Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"), log.TraceId);
    }

    // ─── LogResponse ────────────────────────────────────────────

    [Fact]
    public void LogResponse_logs_a_response_entry()
    {
        var tracker = CreateTracker();
        var correlationId = tracker.LogRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { });

        tracker.LogResponse("Kafka", "OrderService", new Uri("kafka://orders"), correlationId);

        var logs = GetLogsFromThisTest();
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public void LogResponse_uses_same_correlation_id_as_request()
    {
        var tracker = CreateTracker();
        var correlationId = tracker.LogRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { });

        tracker.LogResponse("Kafka", "OrderService", new Uri("kafka://orders"), correlationId);

        var logs = GetLogsFromThisTest();
        Assert.Equal(logs[0].RequestResponseId, logs[1].RequestResponseId);
    }

    [Fact]
    public void LogResponse_serialises_response_payload_when_provided()
    {
        var tracker = CreateTracker();
        var id = tracker.LogRequest("MQ", "Worker", new Uri("mq://queue"), new { });

        tracker.LogResponse("MQ", "Worker", new Uri("mq://queue"), id, new { Ack = true });

        var responseLog = GetLogsFromThisTest().Last();
        Assert.Contains("true", responseLog.Content);
    }

    [Fact]
    public void LogResponse_sets_empty_content_when_no_response_payload()
    {
        var tracker = CreateTracker();
        var id = tracker.LogRequest("MQ", "Worker", new Uri("mq://queue"), new { });

        tracker.LogResponse("MQ", "Worker", new Uri("mq://queue"), id);

        var responseLog = GetLogsFromThisTest().Last();
        Assert.Equal(string.Empty, responseLog.Content);
    }

    [Fact]
    public void LogResponse_sets_meta_type_to_event()
    {
        var tracker = CreateTracker();
        var id = tracker.LogRequest("Kafka", "Svc", new Uri("kafka://t"), new { });

        tracker.LogResponse("Kafka", "Svc", new Uri("kafka://t"), id);

        var responseLog = GetLogsFromThisTest().Last();
        Assert.Equal(RequestResponseMetaType.Event, responseLog.MetaType);
    }
}
