using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Constants;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

public class MessageTrackerTests
{
    private static RequestResponseLog[] GetLogsById(Guid requestResponseId)
    {
        return RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.RequestResponseId == requestResponseId)
            .ToArray();
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
        string callingServiceName = "MyService",
        Func<(string Name, string Id)>? testInfoFallback = null)
    {
        return new MessageTracker(accessor ?? CreateHttpContextAccessor(), callingServiceName, testInfoFallback: testInfoFallback);
    }

    // ─── TrackMessageRequest ────────────────────────────────────

    [Fact]
    public void TrackMessageRequest_logs_a_request_entry()
    {
        var tracker = CreateTracker();

        var id = tracker.TrackMessageRequest("Kafka", "OrderService", new Uri("kafka://orders-topic"), new { Id = 1 });

        var logs = GetLogsById(id);
        Assert.Single(logs);
        Assert.Equal(RequestResponseType.Request, logs[0].Type);
    }

    [Fact]
    public void TrackMessageRequest_returns_a_non_empty_correlation_id()
    {
        var tracker = CreateTracker();

        var id = tracker.TrackMessageRequest("Kafka", "OrderService", new Uri("kafka://orders-topic"), new { Id = 1 });

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public void TrackMessageRequest_sets_protocol_as_method()
    {
        var tracker = CreateTracker();

        var id = tracker.TrackMessageRequest("EventGrid", "NotificationService", new Uri("eventgrid://events"), new { Msg = "hi" });

        var log = GetLogsById(id).Single();
        Assert.Equal("EventGrid", log.Method.Value);
    }

    [Fact]
    public void TrackMessageRequest_sets_destination_as_service_name()
    {
        var tracker = CreateTracker();

        var id = tracker.TrackMessageRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { });

        var log = GetLogsById(id).Single();
        Assert.Equal("OrderService", log.ServiceName);
    }

    [Fact]
    public void TrackMessageRequest_sets_calling_service_name()
    {
        var tracker = CreateTracker(callingServiceName: "PublisherApp");

        var id = tracker.TrackMessageRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { });

        var log = GetLogsById(id).Single();
        Assert.Equal("PublisherApp", log.CallerName);
    }

    [Fact]
    public void TrackMessageRequest_serialises_payload_as_content()
    {
        var tracker = CreateTracker();

        var id = tracker.TrackMessageRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { Name = "Widget" });

        var log = GetLogsById(id).Single();
        Assert.Contains("Widget", log.Content);
    }

    [Fact]
    public void TrackMessageRequest_sets_uri()
    {
        var uri = new Uri("kafka://my-topic");
        var tracker = CreateTracker();

        var id = tracker.TrackMessageRequest("Kafka", "OrderService", uri, new { });

        var log = GetLogsById(id).Single();
        Assert.Equal(uri, log.Uri);
    }

    [Fact]
    public void TrackMessageRequest_sets_meta_type_to_event()
    {
        var tracker = CreateTracker();

        var id = tracker.TrackMessageRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { });

        var log = GetLogsById(id).Single();
        Assert.Equal(RequestResponseMetaType.Event, log.MetaType);
    }

    [Fact]
    public void TrackMessageRequest_reads_test_info_from_http_context()
    {
        var accessor = CreateHttpContextAccessor("SpecificTest", "id-42", "7c9e6679-7425-40de-944b-e07fc1f90ae7");
        var tracker = CreateTracker(accessor);

        var id = tracker.TrackMessageRequest("SNS", "NotifySvc", new Uri("sns://topic"), new { });

        var log = GetLogsById(id).Single();
        Assert.Equal("SpecificTest", log.TestName);
        Assert.Equal("id-42", log.TestId);
        Assert.Equal(Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"), log.TraceId);
    }

    // ─── TrackMessageResponse ───────────────────────────────────

    [Fact]
    public void TrackMessageResponse_logs_a_response_entry()
    {
        var tracker = CreateTracker();
        var correlationId = tracker.TrackMessageRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { });

        tracker.TrackMessageResponse("Kafka", "OrderService", new Uri("kafka://orders"), correlationId);

        var logs = GetLogsById(correlationId);
        Assert.Equal(2, logs.Length);
        Assert.Equal(RequestResponseType.Response, logs[1].Type);
    }

    [Fact]
    public void TrackMessageResponse_uses_same_correlation_id_as_request()
    {
        var tracker = CreateTracker();
        var correlationId = tracker.TrackMessageRequest("Kafka", "OrderService", new Uri("kafka://orders"), new { });

        tracker.TrackMessageResponse("Kafka", "OrderService", new Uri("kafka://orders"), correlationId);

        var logs = GetLogsById(correlationId);
        Assert.Equal(logs[0].RequestResponseId, logs[1].RequestResponseId);
    }

    [Fact]
    public void TrackMessageResponse_serialises_response_payload_when_provided()
    {
        var tracker = CreateTracker();
        var id = tracker.TrackMessageRequest("MQ", "Worker", new Uri("mq://queue"), new { });

        tracker.TrackMessageResponse("MQ", "Worker", new Uri("mq://queue"), id, new { Ack = true });

        var responseLog = GetLogsById(id).Last();
        Assert.Contains("true", responseLog.Content);
    }

    [Fact]
    public void TrackMessageResponse_sets_empty_content_when_no_response_payload()
    {
        var tracker = CreateTracker();
        var id = tracker.TrackMessageRequest("MQ", "Worker", new Uri("mq://queue"), new { });

        tracker.TrackMessageResponse("MQ", "Worker", new Uri("mq://queue"), id);

        var responseLog = GetLogsById(id).Last();
        Assert.Equal(string.Empty, responseLog.Content);
    }

    [Fact]
    public void TrackMessageResponse_sets_meta_type_to_event()
    {
        var tracker = CreateTracker();
        var id = tracker.TrackMessageRequest("Kafka", "Svc", new Uri("kafka://t"), new { });

        tracker.TrackMessageResponse("Kafka", "Svc", new Uri("kafka://t"), id);

        var responseLog = GetLogsById(id).Last();
        Assert.Equal(RequestResponseMetaType.Event, responseLog.MetaType);
    }

    // ─── Fallback test info ─────────────────────────────────────

    [Fact]
    public void TrackMessageRequest_uses_fallback_when_no_http_context()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var tracker = CreateTracker(accessor, testInfoFallback: () => ("FallbackTest", "fallback-id-1"));

        var id = tracker.TrackMessageRequest("ServiceBus", "Queue", new Uri("servicebus://queue"), new { Msg = "hello" });

        var log = GetLogsById(id).Single();
        Assert.Equal("FallbackTest", log.TestName);
        Assert.Equal("fallback-id-1", log.TestId);
    }

    [Fact]
    public void TrackMessageResponse_uses_fallback_when_no_http_context()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var tracker = CreateTracker(accessor, testInfoFallback: () => ("FallbackTest", "fallback-id-2"));

        var id = tracker.TrackMessageRequest("ServiceBus", "Queue", new Uri("servicebus://queue"), new { });
        tracker.TrackMessageResponse("ServiceBus", "Queue", new Uri("servicebus://queue"), id);

        var logs = GetLogsById(id);
        Assert.Equal(2, logs.Length);
        Assert.All(logs, l => Assert.Equal("FallbackTest", l.TestName));
        Assert.All(logs, l => Assert.Equal("fallback-id-2", l.TestId));
    }

    [Fact]
    public void TrackMessageRequest_prefers_http_context_over_fallback()
    {
        var accessor = CreateHttpContextAccessor("HttpTest", "http-id", "7c9e6679-7425-40de-944b-e07fc1f90ae7");
        var tracker = CreateTracker(accessor, testInfoFallback: () => ("FallbackTest", "fallback-id"));

        var id = tracker.TrackMessageRequest("ServiceBus", "Queue", new Uri("servicebus://queue"), new { });

        var log = GetLogsById(id).Single();
        Assert.Equal("HttpTest", log.TestName);
        Assert.Equal("http-id", log.TestId);
    }

    [Fact]
    public void TrackMessageRequest_throws_when_no_http_context_and_no_fallback()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var tracker = CreateTracker(accessor);

        Assert.Throws<InvalidOperationException>(() =>
            tracker.TrackMessageRequest("ServiceBus", "Queue", new Uri("servicebus://queue"), new { }));
    }
}
