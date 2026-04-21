using TestTrackingDiagrams.Extensions.EventHubs;

namespace TestTrackingDiagrams.Tests.EventHubs;

public class EventHubsOperationClassifierTests
{
    // ─── Operation classification ────────────────────────────

    [Fact]
    public void Classify_SendAsync_SingleEvent_ReturnsSend()
    {
        var result = EventHubsOperationClassifier.Classify("SendAsync", "telemetry", null, 1);
        Assert.Equal(EventHubsOperation.Send, result.Operation);
    }

    [Fact]
    public void Classify_SendAsync_MultipleEvents_ReturnsSendBatch()
    {
        var result = EventHubsOperationClassifier.Classify("SendAsync", "telemetry", null, 5);
        Assert.Equal(EventHubsOperation.SendBatch, result.Operation);
    }

    [Fact]
    public void Classify_CreateBatchAsync_ReturnsCreateBatch()
    {
        var result = EventHubsOperationClassifier.Classify("CreateBatchAsync", "telemetry");
        Assert.Equal(EventHubsOperation.CreateBatch, result.Operation);
    }

    [Fact]
    public void Classify_ReadEventsAsync_ReturnsReadEvents()
    {
        var result = EventHubsOperationClassifier.Classify("ReadEventsAsync", "telemetry");
        Assert.Equal(EventHubsOperation.ReadEvents, result.Operation);
    }

    [Fact]
    public void Classify_ReadEventsFromPartitionAsync_ReturnsReadEventsFromPartition()
    {
        var result = EventHubsOperationClassifier.Classify("ReadEventsFromPartitionAsync", "telemetry", "0");
        Assert.Equal(EventHubsOperation.ReadEventsFromPartition, result.Operation);
        Assert.Equal("0", result.PartitionId);
    }

    [Fact]
    public void Classify_GetPartitionIdsAsync_ReturnsGetPartitionIds()
    {
        var result = EventHubsOperationClassifier.Classify("GetPartitionIdsAsync", "telemetry");
        Assert.Equal(EventHubsOperation.GetPartitionIds, result.Operation);
    }

    [Fact]
    public void Classify_GetEventHubPropertiesAsync_ReturnsGetEventHubProperties()
    {
        var result = EventHubsOperationClassifier.Classify("GetEventHubPropertiesAsync", "telemetry");
        Assert.Equal(EventHubsOperation.GetEventHubProperties, result.Operation);
    }

    [Fact]
    public void Classify_GetPartitionPropertiesAsync_ReturnsGetPartitionProperties()
    {
        var result = EventHubsOperationClassifier.Classify("GetPartitionPropertiesAsync", "telemetry", "0");
        Assert.Equal(EventHubsOperation.GetPartitionProperties, result.Operation);
    }

    [Fact]
    public void Classify_StartProcessingAsync_ReturnsStartProcessing()
    {
        var result = EventHubsOperationClassifier.Classify("StartProcessingAsync", "telemetry");
        Assert.Equal(EventHubsOperation.StartProcessing, result.Operation);
    }

    [Fact]
    public void Classify_StopProcessingAsync_ReturnsStopProcessing()
    {
        var result = EventHubsOperationClassifier.Classify("StopProcessingAsync", "telemetry");
        Assert.Equal(EventHubsOperation.StopProcessing, result.Operation);
    }

    [Fact]
    public void Classify_ProcessEvent_ReturnsProcessEvent()
    {
        var result = EventHubsOperationClassifier.Classify("ProcessEvent", "telemetry");
        Assert.Equal(EventHubsOperation.ProcessEvent, result.Operation);
    }

    [Fact]
    public void Classify_UnknownMethod_ReturnsOther()
    {
        var result = EventHubsOperationClassifier.Classify("UnknownMethod", "telemetry");
        Assert.Equal(EventHubsOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_CapturesEventHubName()
    {
        var result = EventHubsOperationClassifier.Classify("SendAsync", "my-hub", null, 1);
        Assert.Equal("my-hub", result.EventHubName);
    }

    [Fact]
    public void Classify_CapturesEventCount()
    {
        var result = EventHubsOperationClassifier.Classify("SendAsync", "hub", null, 42);
        Assert.Equal(42, result.EventCount);
    }

    // ─── Diagram labels ─────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Detailed_Send_IncludesHubName()
    {
        var op = new EventHubsOperationInfo(EventHubsOperation.Send, "telemetry", null, 1);
        var label = EventHubsOperationClassifier.GetDiagramLabel(op, EventHubsTrackingVerbosity.Detailed);
        Assert.Equal("Send → telemetry", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_SendBatch_IncludesCount()
    {
        var op = new EventHubsOperationInfo(EventHubsOperation.SendBatch, "telemetry", null, 5);
        var label = EventHubsOperationClassifier.GetDiagramLabel(op, EventHubsTrackingVerbosity.Detailed);
        Assert.Equal("Send (×5) → telemetry", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_ReadEventsFromPartition_IncludesPartition()
    {
        var op = new EventHubsOperationInfo(EventHubsOperation.ReadEventsFromPartition, "telemetry", "2");
        var label = EventHubsOperationClassifier.GetDiagramLabel(op, EventHubsTrackingVerbosity.Detailed);
        Assert.Equal("Read ← telemetry[2]", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_SendBatch_SimplifiesToSend()
    {
        var op = new EventHubsOperationInfo(EventHubsOperation.SendBatch, "telemetry", null, 5);
        var label = EventHubsOperationClassifier.GetDiagramLabel(op, EventHubsTrackingVerbosity.Summarised);
        Assert.Equal("Send", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_ReadEventsFromPartition_SimplifiesToRead()
    {
        var op = new EventHubsOperationInfo(EventHubsOperation.ReadEventsFromPartition, "telemetry", "0");
        var label = EventHubsOperationClassifier.GetDiagramLabel(op, EventHubsTrackingVerbosity.Summarised);
        Assert.Equal("Read", label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_IncludesAllDetails()
    {
        var op = new EventHubsOperationInfo(EventHubsOperation.Send, "telemetry", "1", 3);
        var label = EventHubsOperationClassifier.GetDiagramLabel(op, EventHubsTrackingVerbosity.Raw);
        Assert.Contains("telemetry", label);
        Assert.Contains("1", label);
        Assert.Contains("3", label);
    }
}
