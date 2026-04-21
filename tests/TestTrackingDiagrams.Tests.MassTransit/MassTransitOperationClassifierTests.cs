using TestTrackingDiagrams.Extensions.MassTransit;

namespace TestTrackingDiagrams.Tests.MassTransit;

public class MassTransitOperationClassifierTests
{
    // ──────────────────────────────────────────────────────────
    //  Diagram labels — Detailed
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Detailed_Send_IncludesMessageType()
    {
        var op = new MassTransitOperationInfo(MassTransitOperation.Send, "OrderCreated",
            new Uri("rabbitmq://localhost/orders-queue"));

        var label = MassTransitOperationClassifier.GetDiagramLabel(op, MassTransitTrackingVerbosity.Detailed);

        Assert.Equal("Send OrderCreated", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Publish_IncludesMessageType()
    {
        var op = new MassTransitOperationInfo(MassTransitOperation.Publish, "UserRegistered",
            new Uri("rabbitmq://localhost/user-events"));

        var label = MassTransitOperationClassifier.GetDiagramLabel(op, MassTransitTrackingVerbosity.Detailed);

        Assert.Equal("Publish UserRegistered", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Consume_IncludesMessageType()
    {
        var op = new MassTransitOperationInfo(MassTransitOperation.Consume, "OrderCreated",
            new Uri("rabbitmq://localhost/orders-queue"));

        var label = MassTransitOperationClassifier.GetDiagramLabel(op, MassTransitTrackingVerbosity.Detailed);

        Assert.Equal("Consume OrderCreated", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_SendFault()
    {
        var op = new MassTransitOperationInfo(MassTransitOperation.SendFault, "OrderCreated");

        var label = MassTransitOperationClassifier.GetDiagramLabel(op, MassTransitTrackingVerbosity.Detailed);

        Assert.Equal("Send Fault OrderCreated", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_PublishFault()
    {
        var op = new MassTransitOperationInfo(MassTransitOperation.PublishFault, "OrderCreated");

        var label = MassTransitOperationClassifier.GetDiagramLabel(op, MassTransitTrackingVerbosity.Detailed);

        Assert.Equal("Publish Fault OrderCreated", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_ConsumeFault()
    {
        var op = new MassTransitOperationInfo(MassTransitOperation.ConsumeFault, "OrderCreated");

        var label = MassTransitOperationClassifier.GetDiagramLabel(op, MassTransitTrackingVerbosity.Detailed);

        Assert.Equal("Consume Fault OrderCreated", label);
    }

    // ──────────────────────────────────────────────────────────
    //  Diagram labels — Summarised
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Summarised_Send_UsesArrow()
    {
        var op = new MassTransitOperationInfo(MassTransitOperation.Send, "OrderCreated");

        var label = MassTransitOperationClassifier.GetDiagramLabel(op, MassTransitTrackingVerbosity.Summarised);

        Assert.Equal("→ OrderCreated", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_Publish_UsesArrow()
    {
        var op = new MassTransitOperationInfo(MassTransitOperation.Publish, "OrderCreated");

        var label = MassTransitOperationClassifier.GetDiagramLabel(op, MassTransitTrackingVerbosity.Summarised);

        Assert.Equal("→ OrderCreated", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_Consume_UsesReverseArrow()
    {
        var op = new MassTransitOperationInfo(MassTransitOperation.Consume, "OrderCreated");

        var label = MassTransitOperationClassifier.GetDiagramLabel(op, MassTransitTrackingVerbosity.Summarised);

        Assert.Equal("← OrderCreated", label);
    }

    // ──────────────────────────────────────────────────────────
    //  Diagram labels — Raw
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Raw_IncludesFullDestination()
    {
        var dest = new Uri("rabbitmq://localhost/orders-queue");
        var op = new MassTransitOperationInfo(MassTransitOperation.Send, "OrderCreated", dest);

        var label = MassTransitOperationClassifier.GetDiagramLabel(op, MassTransitTrackingVerbosity.Raw);

        Assert.Equal("Send OrderCreated → rabbitmq://localhost/orders-queue", label);
    }

    // ──────────────────────────────────────────────────────────
    //  URI building
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void BuildUri_Raw_ReturnsDestinationAddress()
    {
        var dest = new Uri("rabbitmq://localhost/orders-queue");
        var op = new MassTransitOperationInfo(MassTransitOperation.Send, "OrderCreated", dest);

        var uri = MassTransitOperationClassifier.BuildUri(op, MassTransitTrackingVerbosity.Raw);

        Assert.Equal(dest, uri);
    }

    [Fact]
    public void BuildUri_Detailed_ExtractsQueueName()
    {
        var dest = new Uri("rabbitmq://localhost/orders-queue");
        var op = new MassTransitOperationInfo(MassTransitOperation.Send, "OrderCreated", dest);

        var uri = MassTransitOperationClassifier.BuildUri(op, MassTransitTrackingVerbosity.Detailed);

        Assert.Equal("masstransit:///orders-queue", uri.ToString());
    }

    [Fact]
    public void BuildUri_Summarised_UsesMessageType()
    {
        var op = new MassTransitOperationInfo(MassTransitOperation.Send, "OrderCreated",
            new Uri("rabbitmq://localhost/orders-queue"));

        var uri = MassTransitOperationClassifier.BuildUri(op, MassTransitTrackingVerbosity.Summarised);

        Assert.Equal("masstransit:///OrderCreated", uri.ToString());
    }

    [Fact]
    public void BuildUri_Detailed_NoDestination_ReturnsUnknown()
    {
        var op = new MassTransitOperationInfo(MassTransitOperation.Consume, "OrderCreated");

        var uri = MassTransitOperationClassifier.BuildUri(op, MassTransitTrackingVerbosity.Detailed);

        Assert.Equal("masstransit:///unknown", uri.ToString());
    }

    [Fact]
    public void BuildUri_Detailed_AzureServiceBusUri_ExtractsQueueName()
    {
        var dest = new Uri("sb://my-namespace.servicebus.windows.net/orders-queue");
        var op = new MassTransitOperationInfo(MassTransitOperation.Send, "OrderCreated", dest);

        var uri = MassTransitOperationClassifier.BuildUri(op, MassTransitTrackingVerbosity.Detailed);

        Assert.Equal("masstransit:///orders-queue", uri.ToString());
    }

    [Fact]
    public void BuildUri_Detailed_SqsUri_ExtractsQueueName()
    {
        var dest = new Uri("amazonsqs://us-east-1/my-queue");
        var op = new MassTransitOperationInfo(MassTransitOperation.Send, "OrderCreated", dest);

        var uri = MassTransitOperationClassifier.BuildUri(op, MassTransitTrackingVerbosity.Detailed);

        Assert.Equal("masstransit:///my-queue", uri.ToString());
    }
}
