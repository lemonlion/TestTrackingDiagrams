using TestTrackingDiagrams.Extensions.StorageQueues;

namespace TestTrackingDiagrams.Tests.StorageQueues;

public class StorageQueueOperationClassifierTests
{
    // ──────────────────────────────────────────────────────────
    //  Message operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_PostMessages_ReturnsSendMessage()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://account.queue.core.windows.net/my-queue/messages");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.SendMessage, result.Operation);
        Assert.Equal("my-queue", result.QueueName);
    }

    [Fact]
    public void Classify_GetMessages_ReturnsReceiveMessages()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.queue.core.windows.net/my-queue/messages");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.ReceiveMessages, result.Operation);
        Assert.Equal("my-queue", result.QueueName);
    }

    [Fact]
    public void Classify_GetMessagesWithPeekonly_ReturnsPeekMessages()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.queue.core.windows.net/my-queue/messages?peekonly=true");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.PeekMessages, result.Operation);
        Assert.Equal("my-queue", result.QueueName);
    }

    [Fact]
    public void Classify_DeleteMessageWithId_ReturnsDeleteMessage()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://account.queue.core.windows.net/my-queue/messages/msg-123?popreceipt=abc");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.DeleteMessage, result.Operation);
        Assert.Equal("my-queue", result.QueueName);
        Assert.Equal("msg-123", result.MessageId);
    }

    [Fact]
    public void Classify_PutMessageWithId_ReturnsUpdateMessage()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://account.queue.core.windows.net/my-queue/messages/msg-456?popreceipt=xyz&visibilitytimeout=30");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.UpdateMessage, result.Operation);
        Assert.Equal("my-queue", result.QueueName);
        Assert.Equal("msg-456", result.MessageId);
    }

    [Fact]
    public void Classify_DeleteMessages_ReturnsClearMessages()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://account.queue.core.windows.net/my-queue/messages");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.ClearMessages, result.Operation);
        Assert.Equal("my-queue", result.QueueName);
    }

    // ──────────────────────────────────────────────────────────
    //  Queue operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_PutQueue_ReturnsCreateQueue()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://account.queue.core.windows.net/my-queue");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.CreateQueue, result.Operation);
        Assert.Equal("my-queue", result.QueueName);
    }

    [Fact]
    public void Classify_DeleteQueue_ReturnsDeleteQueue()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://account.queue.core.windows.net/my-queue");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.DeleteQueue, result.Operation);
        Assert.Equal("my-queue", result.QueueName);
    }

    [Fact]
    public void Classify_GetQueueWithCompMetadata_ReturnsGetProperties()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.queue.core.windows.net/my-queue?comp=metadata");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.GetProperties, result.Operation);
        Assert.Equal("my-queue", result.QueueName);
    }

    [Fact]
    public void Classify_PutQueueWithCompMetadata_ReturnsSetMetadata()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://account.queue.core.windows.net/my-queue?comp=metadata");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.SetMetadata, result.Operation);
        Assert.Equal("my-queue", result.QueueName);
    }

    // ──────────────────────────────────────────────────────────
    //  List queues
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_GetRootWithCompList_ReturnsListQueues()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.queue.core.windows.net/?comp=list");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.ListQueues, result.Operation);
        Assert.Null(result.QueueName);
    }

    // ──────────────────────────────────────────────────────────
    //  Edge cases
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_UnrecognisedMethod_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch,
            "https://account.queue.core.windows.net/my-queue/messages");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.Other, result.Operation);
        Assert.Equal("my-queue", result.QueueName);
    }

    [Fact]
    public void Classify_GetQueueWithoutCompMetadata_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.queue.core.windows.net/my-queue");

        var result = StorageQueueOperationClassifier.Classify(request);

        Assert.Equal(StorageQueueOperation.Other, result.Operation);
    }

    // ──────────────────────────────────────────────────────────
    //  Diagram labels
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Detailed_SendMessage_IncludesQueueName()
    {
        var op = new StorageQueueOperationInfo(StorageQueueOperation.SendMessage, "orders");

        var label = StorageQueueOperationClassifier.GetDiagramLabel(op, StorageQueueTrackingVerbosity.Detailed);

        Assert.Equal("Send → orders", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_ReceiveMessages_IncludesQueueName()
    {
        var op = new StorageQueueOperationInfo(StorageQueueOperation.ReceiveMessages, "orders");

        var label = StorageQueueOperationClassifier.GetDiagramLabel(op, StorageQueueTrackingVerbosity.Detailed);

        Assert.Equal("Receive ← orders", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_PeekMessages_IncludesQueueName()
    {
        var op = new StorageQueueOperationInfo(StorageQueueOperation.PeekMessages, "orders");

        var label = StorageQueueOperationClassifier.GetDiagramLabel(op, StorageQueueTrackingVerbosity.Detailed);

        Assert.Equal("Peek ← orders", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_DeleteMessage_ReturnsDelete()
    {
        var op = new StorageQueueOperationInfo(StorageQueueOperation.DeleteMessage, "orders", "msg-1");

        var label = StorageQueueOperationClassifier.GetDiagramLabel(op, StorageQueueTrackingVerbosity.Detailed);

        Assert.Equal("Delete", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_ClearMessages_IncludesQueueName()
    {
        var op = new StorageQueueOperationInfo(StorageQueueOperation.ClearMessages, "orders");

        var label = StorageQueueOperationClassifier.GetDiagramLabel(op, StorageQueueTrackingVerbosity.Detailed);

        Assert.Equal("Clear → orders", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_UsesOperationNameOnly()
    {
        var op = new StorageQueueOperationInfo(StorageQueueOperation.SendMessage, "orders");

        var label = StorageQueueOperationClassifier.GetDiagramLabel(op, StorageQueueTrackingVerbosity.Summarised);

        Assert.Equal("SendMessage", label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_UsesOperationNameOnly()
    {
        var op = new StorageQueueOperationInfo(StorageQueueOperation.SendMessage, "orders");

        var label = StorageQueueOperationClassifier.GetDiagramLabel(op, StorageQueueTrackingVerbosity.Raw);

        Assert.Equal("SendMessage", label);
    }
}
