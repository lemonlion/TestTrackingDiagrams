using TestTrackingDiagrams.Extensions.PubSub;

namespace TestTrackingDiagrams.Tests.PubSub;

public class PubSubOperationClassifierTests
{
    // ─── Classification ─────────────────────────────────────

    [Fact]
    public void Classify_PublishAsync_Single_ReturnsPublish()
    {
        var result = PubSubOperationClassifier.Classify("PublishAsync", "projects/p/topics/t", null, 1);
        Assert.Equal(PubSubOperation.Publish, result.Operation);
    }

    [Fact]
    public void Classify_PublishAsync_Batch_ReturnsPublishBatch()
    {
        var result = PubSubOperationClassifier.Classify("PublishAsync", "projects/p/topics/t", null, 5);
        Assert.Equal(PubSubOperation.PublishBatch, result.Operation);
    }

    [Fact]
    public void Classify_PullAsync_ReturnsPull()
    {
        var result = PubSubOperationClassifier.Classify("PullAsync", null, "projects/p/subscriptions/s", null);
        Assert.Equal(PubSubOperation.Pull, result.Operation);
    }

    [Fact]
    public void Classify_AcknowledgeAsync_ReturnsAcknowledge()
    {
        var result = PubSubOperationClassifier.Classify("AcknowledgeAsync", null, "projects/p/subscriptions/s", null);
        Assert.Equal(PubSubOperation.Acknowledge, result.Operation);
    }

    [Fact]
    public void Classify_Receive_ReturnsReceive()
    {
        var result = PubSubOperationClassifier.Classify("Receive", null, "projects/p/subscriptions/s", 1);
        Assert.Equal(PubSubOperation.Receive, result.Operation);
    }

    [Fact]
    public void Classify_StartAsync_ReturnsStartSubscriber()
    {
        var result = PubSubOperationClassifier.Classify("StartAsync", null, null, null);
        Assert.Equal(PubSubOperation.StartSubscriber, result.Operation);
    }

    [Fact]
    public void Classify_Unknown_ReturnsOther()
    {
        var result = PubSubOperationClassifier.Classify("Unknown", null, null, null);
        Assert.Equal(PubSubOperation.Other, result.Operation);
    }

    // ─── Short name extraction ──────────────────────────────

    [Fact]
    public void Classify_PreservesTopicName()
    {
        var result = PubSubOperationClassifier.Classify("PublishAsync", "projects/my-project/topics/my-topic", null, 1);
        Assert.Equal("projects/my-project/topics/my-topic", result.TopicName);
    }

    [Fact]
    public void Classify_PreservesSubscriptionName()
    {
        var result = PubSubOperationClassifier.Classify("PullAsync", null, "projects/p/subscriptions/my-sub", null);
        Assert.Equal("projects/p/subscriptions/my-sub", result.SubscriptionName);
    }

    // ─── Diagram labels ─────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Detailed_Publish_ShowsShortTopicName()
    {
        var op = new PubSubOperationInfo(PubSubOperation.Publish, "projects/p/topics/my-topic", null, 1);

        var label = PubSubOperationClassifier.GetDiagramLabel(op, PubSubTrackingVerbosity.Detailed);

        Assert.Equal("Publish → my-topic", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_PublishBatch_ShowsCount()
    {
        var op = new PubSubOperationInfo(PubSubOperation.PublishBatch, "projects/p/topics/my-topic", null, 5);

        var label = PubSubOperationClassifier.GetDiagramLabel(op, PubSubTrackingVerbosity.Detailed);

        Assert.Equal("Publish (×5) → my-topic", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Pull_ShowsSubscriptionName()
    {
        var op = new PubSubOperationInfo(PubSubOperation.Pull, null, "projects/p/subscriptions/my-sub", null);

        var label = PubSubOperationClassifier.GetDiagramLabel(op, PubSubTrackingVerbosity.Detailed);

        Assert.Equal("Pull ← my-sub", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Receive_ShowsSubscriptionName()
    {
        var op = new PubSubOperationInfo(PubSubOperation.Receive, null, "projects/p/subscriptions/my-sub", 1);

        var label = PubSubOperationClassifier.GetDiagramLabel(op, PubSubTrackingVerbosity.Detailed);

        Assert.Equal("Receive ← my-sub", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_Publish_ReturnsOperationName()
    {
        var op = new PubSubOperationInfo(PubSubOperation.Publish, "projects/p/topics/t", null, 1);

        var label = PubSubOperationClassifier.GetDiagramLabel(op, PubSubTrackingVerbosity.Summarised);

        Assert.Equal("Publish", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_BatchPublish_CollapsesToPublish()
    {
        var op = new PubSubOperationInfo(PubSubOperation.PublishBatch, "projects/p/topics/t", null, 5);

        var label = PubSubOperationClassifier.GetDiagramLabel(op, PubSubTrackingVerbosity.Summarised);

        Assert.Equal("Publish", label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_IncludesAllInfo()
    {
        var op = new PubSubOperationInfo(PubSubOperation.Publish, "projects/p/topics/t", null, 1);

        var label = PubSubOperationClassifier.GetDiagramLabel(op, PubSubTrackingVerbosity.Raw);

        Assert.Contains("Publish", label);
        Assert.Contains("projects/p/topics/t", label);
    }
}
