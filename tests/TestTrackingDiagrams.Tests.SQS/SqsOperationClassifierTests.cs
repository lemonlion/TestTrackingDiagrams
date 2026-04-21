using TestTrackingDiagrams.Extensions.SQS;

namespace TestTrackingDiagrams.Tests.SQS;

public class SqsOperationClassifierTests
{
    // ─── X-Amz-Target header classification ──────────────────

    [Theory]
    [InlineData("AmazonSQS.SendMessage", SqsOperation.SendMessage)]
    [InlineData("AmazonSQS.SendMessageBatch", SqsOperation.SendMessageBatch)]
    [InlineData("AmazonSQS.ReceiveMessage", SqsOperation.ReceiveMessage)]
    [InlineData("AmazonSQS.DeleteMessage", SqsOperation.DeleteMessage)]
    [InlineData("AmazonSQS.DeleteMessageBatch", SqsOperation.DeleteMessageBatch)]
    [InlineData("AmazonSQS.ChangeMessageVisibility", SqsOperation.ChangeMessageVisibility)]
    [InlineData("AmazonSQS.ChangeMessageVisibilityBatch", SqsOperation.ChangeMessageVisibilityBatch)]
    [InlineData("AmazonSQS.CreateQueue", SqsOperation.CreateQueue)]
    [InlineData("AmazonSQS.DeleteQueue", SqsOperation.DeleteQueue)]
    [InlineData("AmazonSQS.GetQueueUrl", SqsOperation.GetQueueUrl)]
    [InlineData("AmazonSQS.GetQueueAttributes", SqsOperation.GetQueueAttributes)]
    [InlineData("AmazonSQS.SetQueueAttributes", SqsOperation.SetQueueAttributes)]
    [InlineData("AmazonSQS.PurgeQueue", SqsOperation.PurgeQueue)]
    [InlineData("AmazonSQS.ListQueues", SqsOperation.ListQueues)]
    public void Classify_XAmzTarget_MapsToCorrectOperation(string targetHeader, SqsOperation expected)
    {
        var request = MakeSqsRequest(targetHeader);

        var result = SqsOperationClassifier.Classify(request);

        Assert.Equal(expected, result.Operation);
    }

    [Fact]
    public void Classify_NoTargetHeader_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");

        var result = SqsOperationClassifier.Classify(request);

        Assert.Equal(SqsOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_UnknownOperation_ReturnsOther()
    {
        var request = MakeSqsRequest("AmazonSQS.UnknownOperation");

        var result = SqsOperationClassifier.Classify(request);

        Assert.Equal(SqsOperation.Other, result.Operation);
    }

    // ─── Legacy query protocol ───────────────────────────────

    [Fact]
    public void Classify_ActionQueryParameter_MapsCorrectly()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://sqs.us-east-1.amazonaws.com/123456789/my-queue?Action=SendMessage");

        var result = SqsOperationClassifier.Classify(request);

        Assert.Equal(SqsOperation.SendMessage, result.Operation);
    }

    [Fact]
    public void Classify_ActionInFormBody_MapsCorrectly()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        var body = "Action=ReceiveMessage&QueueUrl=https://sqs.us-east-1.amazonaws.com/123456789/orders";

        var result = SqsOperationClassifier.Classify(request, body);

        Assert.Equal(SqsOperation.ReceiveMessage, result.Operation);
    }

    // ─── Queue name extraction ───────────────────────────────

    [Fact]
    public void Classify_ExtractsQueueNameFromUrlPath()
    {
        var request = MakeSqsRequest("AmazonSQS.SendMessage",
            "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue");

        var result = SqsOperationClassifier.Classify(request);

        Assert.Equal("my-queue", result.QueueName);
    }

    [Fact]
    public void Classify_ExtractsQueueNameFromBodyQueueUrl()
    {
        var request = MakeSqsRequest("AmazonSQS.SendMessage");
        var body = """{"QueueUrl": "https://sqs.us-east-1.amazonaws.com/123456789012/orders-queue", "MessageBody": "hello"}""";

        var result = SqsOperationClassifier.Classify(request, body);

        Assert.Equal("orders-queue", result.QueueName);
    }

    [Fact]
    public void Classify_ExtractsQueueNameFromBodyQueueName()
    {
        var request = MakeSqsRequest("AmazonSQS.CreateQueue");
        var body = """{"QueueName": "new-queue"}""";

        var result = SqsOperationClassifier.Classify(request, body);

        Assert.Equal("new-queue", result.QueueName);
    }

    [Fact]
    public void Classify_FifoQueuePreservesSuffix()
    {
        var request = MakeSqsRequest("AmazonSQS.SendMessage",
            "https://sqs.us-east-1.amazonaws.com/123456789012/orders.fifo");

        var result = SqsOperationClassifier.Classify(request);

        Assert.Equal("orders.fifo", result.QueueName);
    }

    [Fact]
    public void Classify_NoQueueInfoAvailable_QueueNameIsNull()
    {
        var request = MakeSqsRequest("AmazonSQS.ListQueues");

        var result = SqsOperationClassifier.Classify(request);

        Assert.Null(result.QueueName);
    }

    // ─── Diagram labels ─────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Detailed_ReturnsOperationName()
    {
        var op = new SqsOperationInfo(SqsOperation.SendMessage, "my-queue");

        var label = SqsOperationClassifier.GetDiagramLabel(op, SqsTrackingVerbosity.Detailed);

        Assert.Equal("SendMessage", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_ReturnsOperationName()
    {
        var op = new SqsOperationInfo(SqsOperation.ReceiveMessage, "my-queue");

        var label = SqsOperationClassifier.GetDiagramLabel(op, SqsTrackingVerbosity.Summarised);

        Assert.Equal("ReceiveMessage", label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_ReturnsNull()
    {
        var op = new SqsOperationInfo(SqsOperation.SendMessage, "my-queue");

        var label = SqsOperationClassifier.GetDiagramLabel(op, SqsTrackingVerbosity.Raw);

        Assert.Null(label);
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static HttpRequestMessage MakeSqsRequest(string targetHeader,
        string url = "https://sqs.us-east-1.amazonaws.com/")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-Amz-Target", targetHeader);
        return request;
    }
}
