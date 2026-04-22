using TestTrackingDiagrams.Extensions.EventBridge;

namespace TestTrackingDiagrams.Tests.EventBridge;

public class EventBridgeOperationClassifierTests
{
    // ─── X-Amz-Target header classification ──────────────────

    [Theory]
    [InlineData("AWSEvents.PutEvents", EventBridgeOperation.PutEvents)]
    [InlineData("AWSEvents.PutPartnerEvents", EventBridgeOperation.PutPartnerEvents)]
    [InlineData("AWSEvents.TestEventPattern", EventBridgeOperation.TestEventPattern)]
    [InlineData("AWSEvents.PutRule", EventBridgeOperation.PutRule)]
    [InlineData("AWSEvents.DeleteRule", EventBridgeOperation.DeleteRule)]
    [InlineData("AWSEvents.DescribeRule", EventBridgeOperation.DescribeRule)]
    [InlineData("AWSEvents.EnableRule", EventBridgeOperation.EnableRule)]
    [InlineData("AWSEvents.DisableRule", EventBridgeOperation.DisableRule)]
    [InlineData("AWSEvents.ListRules", EventBridgeOperation.ListRules)]
    [InlineData("AWSEvents.PutTargets", EventBridgeOperation.PutTargets)]
    [InlineData("AWSEvents.RemoveTargets", EventBridgeOperation.RemoveTargets)]
    [InlineData("AWSEvents.ListTargetsByRule", EventBridgeOperation.ListTargetsByRule)]
    [InlineData("AWSEvents.CreateEventBus", EventBridgeOperation.CreateEventBus)]
    [InlineData("AWSEvents.DeleteEventBus", EventBridgeOperation.DeleteEventBus)]
    [InlineData("AWSEvents.DescribeEventBus", EventBridgeOperation.DescribeEventBus)]
    [InlineData("AWSEvents.ListEventBuses", EventBridgeOperation.ListEventBuses)]
    [InlineData("AWSEvents.CreateArchive", EventBridgeOperation.CreateArchive)]
    [InlineData("AWSEvents.DeleteArchive", EventBridgeOperation.DeleteArchive)]
    [InlineData("AWSEvents.DescribeArchive", EventBridgeOperation.DescribeArchive)]
    [InlineData("AWSEvents.ListArchives", EventBridgeOperation.ListArchives)]
    [InlineData("AWSEvents.StartReplay", EventBridgeOperation.StartReplay)]
    [InlineData("AWSEvents.DescribeReplay", EventBridgeOperation.DescribeReplay)]
    [InlineData("AWSEvents.ListReplays", EventBridgeOperation.ListReplays)]
    [InlineData("AWSEvents.CreateApiDestination", EventBridgeOperation.CreateApiDestination)]
    [InlineData("AWSEvents.CreateConnection", EventBridgeOperation.CreateConnection)]
    [InlineData("AWSEvents.TagResource", EventBridgeOperation.TagResource)]
    [InlineData("AWSEvents.UntagResource", EventBridgeOperation.UntagResource)]
    [InlineData("AWSEvents.ListTagsForResource", EventBridgeOperation.ListTagsForResource)]
    public void Classify_XAmzTarget_MapsToCorrectOperation(string targetHeader, EventBridgeOperation expected)
    {
        var request = MakeEventBridgeRequest(targetHeader);

        var result = EventBridgeOperationClassifier.Classify(request);

        Assert.Equal(expected, result.Operation);
    }

    [Fact]
    public void Classify_NoTargetHeader_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://events.us-east-1.amazonaws.com/");

        var result = EventBridgeOperationClassifier.Classify(request);

        Assert.Equal(EventBridgeOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_UnknownTarget_ReturnsOther()
    {
        var request = MakeEventBridgeRequest("AWSEvents.UnknownOperation");

        var result = EventBridgeOperationClassifier.Classify(request);

        Assert.Equal(EventBridgeOperation.Other, result.Operation);
    }

    // ─── PutEvents body parsing ──────────────────────────────

    [Fact]
    public void Classify_PutEvents_ExtractsEventBusNameFromBody()
    {
        var request = MakeEventBridgeRequest("AWSEvents.PutEvents");
        var body = """
        {
            "Entries": [
                {
                    "Source": "my.service",
                    "DetailType": "OrderCreated",
                    "Detail": "{}",
                    "EventBusName": "my-event-bus"
                }
            ]
        }
        """;

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal("my-event-bus", result.EventBusName);
    }

    [Fact]
    public void Classify_PutEvents_ExtractsDetailTypeFromFirstEntry()
    {
        var request = MakeEventBridgeRequest("AWSEvents.PutEvents");
        var body = """
        {
            "Entries": [
                { "DetailType": "OrderCreated", "Source": "my.service", "Detail": "{}" }
            ]
        }
        """;

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal("OrderCreated", result.DetailType);
    }

    [Fact]
    public void Classify_PutEvents_ExtractsSourceFromFirstEntry()
    {
        var request = MakeEventBridgeRequest("AWSEvents.PutEvents");
        var body = """
        {
            "Entries": [
                { "DetailType": "OrderCreated", "Source": "my.service", "Detail": "{}" }
            ]
        }
        """;

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal("my.service", result.Source);
    }

    [Fact]
    public void Classify_PutEvents_ExtractsEntryCount()
    {
        var request = MakeEventBridgeRequest("AWSEvents.PutEvents");
        var body = """
        {
            "Entries": [
                { "DetailType": "OrderCreated", "Source": "a", "Detail": "{}" },
                { "DetailType": "OrderUpdated", "Source": "b", "Detail": "{}" },
                { "DetailType": "OrderShipped", "Source": "c", "Detail": "{}" }
            ]
        }
        """;

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal(3, result.EntryCount);
    }

    [Fact]
    public void Classify_PutEvents_EventBusNameFromEntryWhenNotTopLevel()
    {
        var request = MakeEventBridgeRequest("AWSEvents.PutEvents");
        var body = """
        {
            "Entries": [
                {
                    "Source": "my.service",
                    "DetailType": "OrderCreated",
                    "Detail": "{}",
                    "EventBusName": "orders-bus"
                }
            ]
        }
        """;

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal("orders-bus", result.EventBusName);
    }

    [Fact]
    public void Classify_PutEvents_EmptyEntries_EntryCountIsZero()
    {
        var request = MakeEventBridgeRequest("AWSEvents.PutEvents");
        var body = """{ "Entries": [] }""";

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal(0, result.EntryCount);
    }

    [Fact]
    public void Classify_PutEvents_MalformedJson_ReturnsOperationOnly()
    {
        var request = MakeEventBridgeRequest("AWSEvents.PutEvents");
        var body = "not valid json";

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal(EventBridgeOperation.PutEvents, result.Operation);
        Assert.Null(result.EventBusName);
    }

    [Fact]
    public void Classify_PutEvents_NoBody_ReturnsOperationOnly()
    {
        var request = MakeEventBridgeRequest("AWSEvents.PutEvents");

        var result = EventBridgeOperationClassifier.Classify(request);

        Assert.Equal(EventBridgeOperation.PutEvents, result.Operation);
        Assert.Null(result.EntryCount);
    }

    // ─── PutRule body parsing ────────────────────────────────

    [Fact]
    public void Classify_PutRule_ExtractsRuleName()
    {
        var request = MakeEventBridgeRequest("AWSEvents.PutRule");
        var body = """{ "Name": "my-rule", "EventBusName": "my-bus", "State": "ENABLED" }""";

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal("my-rule", result.RuleName);
    }

    [Fact]
    public void Classify_PutRule_ExtractsEventBusName()
    {
        var request = MakeEventBridgeRequest("AWSEvents.PutRule");
        var body = """{ "Name": "my-rule", "EventBusName": "orders-bus" }""";

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal("orders-bus", result.EventBusName);
    }

    [Fact]
    public void Classify_DeleteRule_ExtractsRuleName()
    {
        var request = MakeEventBridgeRequest("AWSEvents.DeleteRule");
        var body = """{ "Name": "old-rule", "EventBusName": "default" }""";

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal("old-rule", result.RuleName);
    }

    [Fact]
    public void Classify_DescribeRule_ExtractsRuleName()
    {
        var request = MakeEventBridgeRequest("AWSEvents.DescribeRule");
        var body = """{ "Name": "my-rule" }""";

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal("my-rule", result.RuleName);
    }

    // ─── Non-body-parsed operations ──────────────────────────

    [Fact]
    public void Classify_ListRules_DoesNotParseBody()
    {
        var request = MakeEventBridgeRequest("AWSEvents.ListRules");
        var body = """{ "EventBusName": "my-bus" }""";

        var result = EventBridgeOperationClassifier.Classify(request, body);

        Assert.Equal(EventBridgeOperation.ListRules, result.Operation);
        Assert.Null(result.RuleName);
        Assert.Null(result.EventBusName);
    }

    // ─── Diagram labels ─────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Raw_IncludesAllDetails()
    {
        var op = new EventBridgeOperationInfo(EventBridgeOperation.PutEvents, "my-bus", DetailType: "OrderCreated", EntryCount: 3);

        var label = EventBridgeOperationClassifier.GetDiagramLabel(op, EventBridgeTrackingVerbosity.Raw);

        Assert.Contains("PutEvents", label);
        Assert.Contains("my-bus", label);
        Assert.Contains("OrderCreated", label);
        Assert.Contains("3", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_PutEventsWithDetailType_IncludesDetailType()
    {
        var op = new EventBridgeOperationInfo(EventBridgeOperation.PutEvents, DetailType: "OrderCreated");

        var label = EventBridgeOperationClassifier.GetDiagramLabel(op, EventBridgeTrackingVerbosity.Detailed);

        Assert.Equal("PutEvents [OrderCreated]", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_PutEventsMultiple_IncludesCount()
    {
        var op = new EventBridgeOperationInfo(EventBridgeOperation.PutEvents, DetailType: "OrderCreated", EntryCount: 5);

        var label = EventBridgeOperationClassifier.GetDiagramLabel(op, EventBridgeTrackingVerbosity.Detailed);

        Assert.Equal("PutEvents [OrderCreated] x5", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_PutEventsNoDetailType_ShowsCount()
    {
        var op = new EventBridgeOperationInfo(EventBridgeOperation.PutEvents, EntryCount: 2);

        var label = EventBridgeOperationClassifier.GetDiagramLabel(op, EventBridgeTrackingVerbosity.Detailed);

        Assert.Equal("PutEvents x2", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_PutRule_IncludesRuleName()
    {
        var op = new EventBridgeOperationInfo(EventBridgeOperation.PutRule, RuleName: "my-rule");

        var label = EventBridgeOperationClassifier.GetDiagramLabel(op, EventBridgeTrackingVerbosity.Detailed);

        Assert.Equal("PutRule my-rule", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_CreateEventBus_IncludesBusName()
    {
        var op = new EventBridgeOperationInfo(EventBridgeOperation.CreateEventBus, EventBusName: "orders-bus");

        var label = EventBridgeOperationClassifier.GetDiagramLabel(op, EventBridgeTrackingVerbosity.Detailed);

        Assert.Equal("CreateEventBus orders-bus", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_PutEvents_SimpleName()
    {
        var op = new EventBridgeOperationInfo(EventBridgeOperation.PutEvents, "my-bus", DetailType: "OrderCreated", EntryCount: 5);

        var label = EventBridgeOperationClassifier.GetDiagramLabel(op, EventBridgeTrackingVerbosity.Summarised);

        Assert.Equal("PutEvents", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_PutRule_GroupsAsManageRule()
    {
        var op = new EventBridgeOperationInfo(EventBridgeOperation.PutRule, RuleName: "my-rule");

        var label = EventBridgeOperationClassifier.GetDiagramLabel(op, EventBridgeTrackingVerbosity.Summarised);

        Assert.Equal("ManageRule", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_DeleteRule_GroupsAsManageRule()
    {
        var op = new EventBridgeOperationInfo(EventBridgeOperation.DeleteRule, RuleName: "old-rule");

        var label = EventBridgeOperationClassifier.GetDiagramLabel(op, EventBridgeTrackingVerbosity.Summarised);

        Assert.Equal("ManageRule", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_PutTargets_GroupsAsManageTargets()
    {
        var op = new EventBridgeOperationInfo(EventBridgeOperation.PutTargets);

        var label = EventBridgeOperationClassifier.GetDiagramLabel(op, EventBridgeTrackingVerbosity.Summarised);

        Assert.Equal("ManageTargets", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_CreateEventBus_GroupsAsManageBus()
    {
        var op = new EventBridgeOperationInfo(EventBridgeOperation.CreateEventBus, EventBusName: "my-bus");

        var label = EventBridgeOperationClassifier.GetDiagramLabel(op, EventBridgeTrackingVerbosity.Summarised);

        Assert.Equal("ManageBus", label);
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static HttpRequestMessage MakeEventBridgeRequest(string targetHeader,
        string url = "https://events.us-east-1.amazonaws.com/")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-Amz-Target", targetHeader);
        return request;
    }
}
