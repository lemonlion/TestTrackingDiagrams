using TestTrackingDiagrams.Extensions.ServiceBus;

namespace TestTrackingDiagrams.Tests.ServiceBus;

public class ServiceBusOperationClassifierTests
{
    // ─── Classify: method name → operation mapping ─────────────

    [Theory]
    [InlineData("SendMessageAsync", ServiceBusOperation.Send)]
    [InlineData("SendMessagesAsync", ServiceBusOperation.SendBatch)]
    [InlineData("ScheduleMessageAsync", ServiceBusOperation.Schedule)]
    [InlineData("ScheduleMessagesAsync", ServiceBusOperation.Schedule)]
    [InlineData("CancelScheduledMessageAsync", ServiceBusOperation.CancelSchedule)]
    [InlineData("CancelScheduledMessagesAsync", ServiceBusOperation.CancelSchedule)]
    [InlineData("ReceiveMessageAsync", ServiceBusOperation.Receive)]
    [InlineData("ReceiveMessagesAsync", ServiceBusOperation.ReceiveBatch)]
    [InlineData("PeekMessageAsync", ServiceBusOperation.Peek)]
    [InlineData("PeekMessagesAsync", ServiceBusOperation.Peek)]
    [InlineData("CompleteMessageAsync", ServiceBusOperation.Complete)]
    [InlineData("AbandonMessageAsync", ServiceBusOperation.Abandon)]
    [InlineData("DeadLetterMessageAsync", ServiceBusOperation.DeadLetter)]
    [InlineData("DeferMessageAsync", ServiceBusOperation.Defer)]
    [InlineData("RenewMessageLockAsync", ServiceBusOperation.RenewMessageLock)]
    [InlineData("RenewSessionLockAsync", ServiceBusOperation.RenewSessionLock)]
    [InlineData("GetSessionStateAsync", ServiceBusOperation.GetSessionState)]
    [InlineData("SetSessionStateAsync", ServiceBusOperation.SetSessionState)]
    [InlineData("StartProcessingAsync", ServiceBusOperation.StartProcessing)]
    [InlineData("StopProcessingAsync", ServiceBusOperation.StopProcessing)]
    public void Classify_MethodName_MapsToCorrectOperation(string methodName, ServiceBusOperation expected)
    {
        var result = ServiceBusOperationClassifier.Classify(methodName, "orders-queue", null);

        Assert.Equal(expected, result.Operation);
    }

    [Fact]
    public void Classify_UnknownMethodName_ReturnsOther()
    {
        var result = ServiceBusOperationClassifier.Classify("SomeUnknownMethod", "orders-queue", null);

        Assert.Equal(ServiceBusOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_ExtractsQueueName()
    {
        var result = ServiceBusOperationClassifier.Classify("SendMessageAsync", "orders-queue", null);

        Assert.Equal("orders-queue", result.QueueOrTopicName);
    }

    [Fact]
    public void Classify_NullEntityPath_ReturnsNullQueueName()
    {
        var result = ServiceBusOperationClassifier.Classify("SendMessageAsync", null, null);

        Assert.Null(result.QueueOrTopicName);
    }

    [Fact]
    public void Classify_EmptyEntityPath_ReturnsNullQueueName()
    {
        var result = ServiceBusOperationClassifier.Classify("SendMessageAsync", "", null);

        Assert.Null(result.QueueOrTopicName);
    }

    [Fact]
    public void Classify_BatchArgs_ExtractsMessageCount()
    {
        var messages = new List<object> { new(), new(), new() };
        var result = ServiceBusOperationClassifier.Classify("SendMessagesAsync", "orders-queue", [messages]);

        Assert.Equal(3, result.MessageCount);
    }

    [Fact]
    public void Classify_NullArgs_MessageCountIsNull()
    {
        var result = ServiceBusOperationClassifier.Classify("SendMessagesAsync", "orders-queue", null);

        Assert.Null(result.MessageCount);
    }

    // ─── GetDiagramLabel: Summarised ───────────────────────────

    [Theory]
    [InlineData(ServiceBusOperation.Send, "Send")]
    [InlineData(ServiceBusOperation.SendBatch, "Send")]
    [InlineData(ServiceBusOperation.Schedule, "Schedule")]
    [InlineData(ServiceBusOperation.CancelSchedule, "CancelSchedule")]
    [InlineData(ServiceBusOperation.Receive, "Receive")]
    [InlineData(ServiceBusOperation.ReceiveBatch, "Receive")]
    [InlineData(ServiceBusOperation.Peek, "Peek")]
    [InlineData(ServiceBusOperation.Complete, "Complete")]
    [InlineData(ServiceBusOperation.Abandon, "Abandon")]
    [InlineData(ServiceBusOperation.DeadLetter, "DeadLetter")]
    [InlineData(ServiceBusOperation.Defer, "Defer")]
    [InlineData(ServiceBusOperation.RenewMessageLock, "RenewLock")]
    [InlineData(ServiceBusOperation.RenewSessionLock, "RenewSessionLock")]
    [InlineData(ServiceBusOperation.GetSessionState, "GetSessionState")]
    [InlineData(ServiceBusOperation.SetSessionState, "SetSessionState")]
    [InlineData(ServiceBusOperation.StartProcessing, "StartProcessing")]
    [InlineData(ServiceBusOperation.StopProcessing, "StopProcessing")]
    [InlineData(ServiceBusOperation.Other, "Other")]
    public void GetDiagramLabel_Summarised_ReturnsSimpleName(ServiceBusOperation operation, string expected)
    {
        var info = new ServiceBusOperationInfo(operation, "orders-queue");

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Summarised);

        Assert.Equal(expected, label);
    }

    // ─── GetDiagramLabel: Detailed ─────────────────────────────

    [Fact]
    public void GetDiagramLabel_Detailed_Send_IncludesQueueName()
    {
        var info = new ServiceBusOperationInfo(ServiceBusOperation.Send, "orders-queue");

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Detailed);

        Assert.Equal("Send → orders-queue", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Send_NullQueue_OmitsArrow()
    {
        var info = new ServiceBusOperationInfo(ServiceBusOperation.Send, null);

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Detailed);

        Assert.Equal("Send", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_SendBatch_IncludesCountAndQueue()
    {
        var info = new ServiceBusOperationInfo(ServiceBusOperation.SendBatch, "orders-queue", MessageCount: 5);

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Detailed);

        Assert.Equal("Send (×5) → orders-queue", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_SendBatch_NoCount_ShowsBatchLabel()
    {
        var info = new ServiceBusOperationInfo(ServiceBusOperation.SendBatch, "orders-queue");

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Detailed);

        Assert.Equal("Send (batch) → orders-queue", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Receive_IncludesQueueName()
    {
        var info = new ServiceBusOperationInfo(ServiceBusOperation.Receive, "orders-queue");

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Detailed);

        Assert.Equal("Receive ← orders-queue", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_ReceiveBatch_IncludesCountAndQueue()
    {
        var info = new ServiceBusOperationInfo(ServiceBusOperation.ReceiveBatch, "orders-queue", MessageCount: 10);

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Detailed);

        Assert.Equal("Receive (×10) ← orders-queue", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Peek_IncludesQueueName()
    {
        var info = new ServiceBusOperationInfo(ServiceBusOperation.Peek, "orders-queue");

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Detailed);

        Assert.Equal("Peek ← orders-queue", label);
    }

    [Theory]
    [InlineData(ServiceBusOperation.Complete, "Complete")]
    [InlineData(ServiceBusOperation.Abandon, "Abandon")]
    [InlineData(ServiceBusOperation.DeadLetter, "DeadLetter")]
    [InlineData(ServiceBusOperation.Defer, "Defer")]
    [InlineData(ServiceBusOperation.RenewMessageLock, "RenewLock")]
    public void GetDiagramLabel_Detailed_ManagementOps_NoQueueInLabel(ServiceBusOperation operation, string expected)
    {
        var info = new ServiceBusOperationInfo(operation, "orders-queue");

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Detailed);

        Assert.Equal(expected, label);
    }

    // ─── GetDiagramLabel: Raw ──────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Raw_ReturnsOperationEnumName()
    {
        var info = new ServiceBusOperationInfo(ServiceBusOperation.Send, "orders-queue");

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Raw);

        Assert.Equal("Send", label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_SendBatch_ReturnsEnumName()
    {
        var info = new ServiceBusOperationInfo(ServiceBusOperation.SendBatch, "orders-queue", MessageCount: 5);

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Raw);

        Assert.Equal("SendBatch", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Schedule_IncludesQueueName()
    {
        var info = new ServiceBusOperationInfo(ServiceBusOperation.Schedule, "delayed-queue");

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Detailed);

        Assert.Equal("Schedule → delayed-queue", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_CancelSchedule_NoQueueInLabel()
    {
        var info = new ServiceBusOperationInfo(ServiceBusOperation.CancelSchedule, "delayed-queue");

        var label = ServiceBusOperationClassifier.GetDiagramLabel(info, ServiceBusTrackingVerbosity.Detailed);

        Assert.Equal("CancelSchedule", label);
    }
}
