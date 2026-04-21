using TestTrackingDiagrams.Extensions.SNS;

namespace TestTrackingDiagrams.Tests.SNS;

public class SnsOperationClassifierTests
{
    // ─── X-Amz-Target header classification ──────────────────

    [Theory]
    [InlineData("AmazonSimpleNotificationService.Publish", SnsOperation.Publish)]
    [InlineData("AmazonSimpleNotificationService.PublishBatch", SnsOperation.PublishBatch)]
    [InlineData("AmazonSimpleNotificationService.Subscribe", SnsOperation.Subscribe)]
    [InlineData("AmazonSimpleNotificationService.Unsubscribe", SnsOperation.Unsubscribe)]
    [InlineData("AmazonSimpleNotificationService.CreateTopic", SnsOperation.CreateTopic)]
    [InlineData("AmazonSimpleNotificationService.DeleteTopic", SnsOperation.DeleteTopic)]
    [InlineData("AmazonSimpleNotificationService.ListTopics", SnsOperation.ListTopics)]
    [InlineData("AmazonSimpleNotificationService.ListSubscriptions", SnsOperation.ListSubscriptions)]
    [InlineData("AmazonSimpleNotificationService.ListSubscriptionsByTopic", SnsOperation.ListSubscriptionsByTopic)]
    [InlineData("AmazonSimpleNotificationService.GetTopicAttributes", SnsOperation.GetTopicAttributes)]
    [InlineData("AmazonSimpleNotificationService.SetTopicAttributes", SnsOperation.SetTopicAttributes)]
    [InlineData("AmazonSimpleNotificationService.ConfirmSubscription", SnsOperation.ConfirmSubscription)]
    public void Classify_XAmzTarget_MapsToCorrectOperation(string targetHeader, SnsOperation expected)
    {
        var request = MakeSnsRequest(targetHeader);

        var result = SnsOperationClassifier.Classify(request);

        Assert.Equal(expected, result.Operation);
    }

    [Fact]
    public void Classify_NoTargetHeader_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sns.us-east-1.amazonaws.com/");

        var result = SnsOperationClassifier.Classify(request);

        Assert.Equal(SnsOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_UnknownOperation_ReturnsOther()
    {
        var request = MakeSnsRequest("AmazonSimpleNotificationService.UnknownOperation");

        var result = SnsOperationClassifier.Classify(request);

        Assert.Equal(SnsOperation.Other, result.Operation);
    }

    // ─── Legacy query protocol ───────────────────────────────

    [Fact]
    public void Classify_ActionQueryParameter_MapsCorrectly()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://sns.us-east-1.amazonaws.com/?Action=Publish");

        var result = SnsOperationClassifier.Classify(request);

        Assert.Equal(SnsOperation.Publish, result.Operation);
    }

    [Fact]
    public void Classify_ActionInFormBody_MapsCorrectly()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sns.us-east-1.amazonaws.com/");
        var body = "Action=Subscribe&TopicArn=arn:aws:sns:us-east-1:123456789:my-topic";

        var result = SnsOperationClassifier.Classify(request, body);

        Assert.Equal(SnsOperation.Subscribe, result.Operation);
    }

    // ─── Topic name extraction ───────────────────────────────

    [Fact]
    public void Classify_ExtractsTopicNameFromTopicArn()
    {
        var request = MakeSnsRequest("AmazonSimpleNotificationService.Publish");
        var body = """{"TopicArn": "arn:aws:sns:us-east-1:123456789012:my-topic", "Message": "hello"}""";

        var result = SnsOperationClassifier.Classify(request, body);

        Assert.Equal("my-topic", result.TopicName);
    }

    [Fact]
    public void Classify_ExtractsTopicNameFromTargetArn()
    {
        var request = MakeSnsRequest("AmazonSimpleNotificationService.Publish");
        var body = """{"TargetArn": "arn:aws:sns:us-east-1:123456789012:endpoint-topic", "Message": "hello"}""";

        var result = SnsOperationClassifier.Classify(request, body);

        Assert.Equal("endpoint-topic", result.TopicName);
    }

    [Fact]
    public void Classify_ExtractsFullArnFromBody()
    {
        var request = MakeSnsRequest("AmazonSimpleNotificationService.Publish");
        var body = """{"TopicArn": "arn:aws:sns:us-east-1:123456789012:my-topic", "Message": "hello"}""";

        var result = SnsOperationClassifier.Classify(request, body);

        Assert.Equal("arn:aws:sns:us-east-1:123456789012:my-topic", result.TopicArn);
    }

    [Fact]
    public void Classify_NoTopicInfoAvailable_TopicNameIsNull()
    {
        var request = MakeSnsRequest("AmazonSimpleNotificationService.ListTopics");

        var result = SnsOperationClassifier.Classify(request);

        Assert.Null(result.TopicName);
    }

    [Fact]
    public void Classify_FifoTopicPreservesSuffix()
    {
        var request = MakeSnsRequest("AmazonSimpleNotificationService.Publish");
        var body = """{"TopicArn": "arn:aws:sns:us-east-1:123456789012:orders.fifo", "Message": "hello"}""";

        var result = SnsOperationClassifier.Classify(request, body);

        Assert.Equal("orders.fifo", result.TopicName);
    }

    // ─── Diagram labels ─────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Detailed_ReturnsOperationName()
    {
        var op = new SnsOperationInfo(SnsOperation.Publish, "my-topic");

        var label = SnsOperationClassifier.GetDiagramLabel(op, SnsTrackingVerbosity.Detailed);

        Assert.Equal("Publish", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_ReturnsOperationName()
    {
        var op = new SnsOperationInfo(SnsOperation.Subscribe, "my-topic");

        var label = SnsOperationClassifier.GetDiagramLabel(op, SnsTrackingVerbosity.Summarised);

        Assert.Equal("Subscribe", label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_ReturnsNull()
    {
        var op = new SnsOperationInfo(SnsOperation.Publish, "my-topic");

        var label = SnsOperationClassifier.GetDiagramLabel(op, SnsTrackingVerbosity.Raw);

        Assert.Null(label);
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static HttpRequestMessage MakeSnsRequest(string targetHeader,
        string url = "https://sns.us-east-1.amazonaws.com/")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-Amz-Target", targetHeader);
        return request;
    }
}
