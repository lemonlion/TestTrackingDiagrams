using System.Text.Json;
using Kronikol.Extensions.CosmosDB;
using Kronikol.Tracking;

namespace Kronikol.Tests.CosmosDB;

[Collection("TestCorrelationStore")]
public class ChangeFeedCorrelationTests : IDisposable
{
    public ChangeFeedCorrelationTests()
    {
        TestCorrelationStore.Clear();
        TestIdentityScope.Reset();
    }

    public void Dispose()
    {
        TestCorrelationStore.Clear();
        TestIdentityScope.Reset();
    }

    private record OrderDocument(string Id, decimal Total);

    [Fact]
    public async Task Wrap_Sets_TestIdentityScope_From_CorrelationStore()
    {
        TestCorrelationStore.Correlate(CorrelationKeys.Cosmos("Orders", "order-1"), "Test A", "id-a");

        (string Name, string Id)? capturedIdentity = null;
        Func<IReadOnlyCollection<OrderDocument>, CancellationToken, Task> originalHandler = (changes, ct) =>
        {
            capturedIdentity = TestIdentityScope.Current;
            return Task.CompletedTask;
        };

        var wrapped = ChangeFeedCorrelation.Wrap(originalHandler, "Orders", doc => doc.Id);
        var changes = new List<OrderDocument> { new("order-1", 42.50m) };

        await wrapped(changes, CancellationToken.None);

        Assert.NotNull(capturedIdentity);
        Assert.Equal("Test A", capturedIdentity.Value.Name);
        Assert.Equal("id-a", capturedIdentity.Value.Id);
    }

    [Fact]
    public async Task Wrap_Restores_Previous_Identity_After_Handler()
    {
        TestCorrelationStore.Correlate(CorrelationKeys.Cosmos("Orders", "order-1"), "Test A", "id-a");

        using var outerScope = TestIdentityScope.Begin("Outer", "outer-id");

        var wrapped = ChangeFeedCorrelation.Wrap<OrderDocument>(
            (_, _) => Task.CompletedTask, "Orders", doc => doc.Id);

        await wrapped([new("order-1", 10m)], CancellationToken.None);

        Assert.Equal("Outer", TestIdentityScope.Current!.Value.Name);
    }

    [Fact]
    public async Task Wrap_NoOp_When_Key_Not_Found()
    {
        (string Name, string Id)? capturedIdentity = null;
        var wrapped = ChangeFeedCorrelation.Wrap<OrderDocument>(
            (_, _) =>
            {
                capturedIdentity = TestIdentityScope.Current;
                return Task.CompletedTask;
            }, "Orders", doc => doc.Id);

        await wrapped([new("unknown-doc", 10m)], CancellationToken.None);

        Assert.Null(capturedIdentity);
    }

    [Fact]
    public async Task Wrap_Uses_First_Match_In_Batch()
    {
        TestCorrelationStore.Correlate(CorrelationKeys.Cosmos("Orders", "order-2"), "Test B", "id-b");

        (string Name, string Id)? capturedIdentity = null;
        var wrapped = ChangeFeedCorrelation.Wrap<OrderDocument>(
            (_, _) =>
            {
                capturedIdentity = TestIdentityScope.Current;
                return Task.CompletedTask;
            }, "Orders", doc => doc.Id);

        await wrapped([new("unknown", 1m), new("order-2", 2m)], CancellationToken.None);

        Assert.NotNull(capturedIdentity);
        Assert.Equal("Test B", capturedIdentity.Value.Name);
    }

    [Fact]
    public async Task Wrap_Reflection_Extracts_Id_Property()
    {
        TestCorrelationStore.Correlate(CorrelationKeys.Cosmos("Orders", "order-1"), "Test A", "id-a");

        (string Name, string Id)? capturedIdentity = null;
        var wrapped = ChangeFeedCorrelation.Wrap<OrderDocument>(
            (_, _) =>
            {
                capturedIdentity = TestIdentityScope.Current;
                return Task.CompletedTask;
            }, "Orders"); // No idSelector — uses reflection

        await wrapped([new("order-1", 42.50m)], CancellationToken.None);

        Assert.NotNull(capturedIdentity);
        Assert.Equal("Test A", capturedIdentity.Value.Name);
    }

    [Fact]
    public async Task WrapJson_Extracts_Id_From_JsonElement()
    {
        TestCorrelationStore.Correlate(CorrelationKeys.Cosmos("Orders", "order-1"), "Test A", "id-a");

        (string Name, string Id)? capturedIdentity = null;
        Func<IReadOnlyCollection<JsonElement>, CancellationToken, Task> handler = (_, _) =>
        {
            capturedIdentity = TestIdentityScope.Current;
            return Task.CompletedTask;
        };

        var wrapped = ChangeFeedCorrelation.WrapJson(handler, "Orders");

        var json = JsonDocument.Parse("""{"id":"order-1","total":42.5}""").RootElement;
        await wrapped([json], CancellationToken.None);

        Assert.NotNull(capturedIdentity);
        Assert.Equal("Test A", capturedIdentity.Value.Name);
    }

    [Fact]
    public async Task Wrap_Scope_Disposed_Even_On_Exception()
    {
        TestCorrelationStore.Correlate(CorrelationKeys.Cosmos("Orders", "order-1"), "Test A", "id-a");

        var wrapped = ChangeFeedCorrelation.Wrap<OrderDocument>(
            (_, _) => throw new InvalidOperationException("boom"),
            "Orders", doc => doc.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => wrapped([new("order-1", 10m)], CancellationToken.None));

        // Scope should be restored (disposed)
        Assert.Null(TestIdentityScope.Current);
    }
}
