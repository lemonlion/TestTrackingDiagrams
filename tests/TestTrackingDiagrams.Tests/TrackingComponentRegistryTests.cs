using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests;

public class TrackingComponentRegistryTests : IDisposable
{
    public TrackingComponentRegistryTests()
    {
        TrackingComponentRegistry.Clear();
    }

    public void Dispose()
    {
        TrackingComponentRegistry.Clear();
    }

    [Fact]
    public void Register_AddsComponentToRegistry()
    {
        var component = new StubTrackingComponent("Test", wasInvoked: false);

        TrackingComponentRegistry.Register(component);

        Assert.Single(TrackingComponentRegistry.GetRegisteredComponents());
    }

    [Fact]
    public void GetUnusedComponents_ReturnsUninvokedComponents()
    {
        var unused = new StubTrackingComponent("Unused", wasInvoked: false);
        var used = new StubTrackingComponent("Used", wasInvoked: true);

        TrackingComponentRegistry.Register(unused);
        TrackingComponentRegistry.Register(used);

        var result = TrackingComponentRegistry.GetUnusedComponents();
        Assert.Single(result);
        Assert.Equal("Unused", result[0].ComponentName);
    }

    [Fact]
    public void GetUnusedComponents_ReturnsEmpty_WhenAllInvoked()
    {
        TrackingComponentRegistry.Register(new StubTrackingComponent("A", wasInvoked: true));
        TrackingComponentRegistry.Register(new StubTrackingComponent("B", wasInvoked: true));

        Assert.Empty(TrackingComponentRegistry.GetUnusedComponents());
    }

    [Fact]
    public void ValidateAllComponentsWereInvoked_Passes_WhenAllInvoked()
    {
        TrackingComponentRegistry.Register(new StubTrackingComponent("A", wasInvoked: true));

        var ex = Record.Exception(() => TrackingComponentRegistry.ValidateAllComponentsWereInvoked());
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateAllComponentsWereInvoked_Passes_WhenEmpty()
    {
        var ex = Record.Exception(() => TrackingComponentRegistry.ValidateAllComponentsWereInvoked());
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateAllComponentsWereInvoked_Throws_WhenComponentUnused()
    {
        TrackingComponentRegistry.Register(new StubTrackingComponent("SqlTrackingInterceptor", wasInvoked: false));

        var ex = Assert.Throws<InvalidOperationException>(TrackingComponentRegistry.ValidateAllComponentsWereInvoked);
        Assert.Contains("SqlTrackingInterceptor", ex.Message);
        Assert.Contains("never invoked", ex.Message);
    }

    [Fact]
    public void ValidateAllComponentsWereInvoked_ListsAllUnusedComponents()
    {
        TrackingComponentRegistry.Register(new StubTrackingComponent("ComponentA", wasInvoked: false));
        TrackingComponentRegistry.Register(new StubTrackingComponent("ComponentB", wasInvoked: false));
        TrackingComponentRegistry.Register(new StubTrackingComponent("ComponentC", wasInvoked: true));

        var ex = Assert.Throws<InvalidOperationException>(TrackingComponentRegistry.ValidateAllComponentsWereInvoked);
        Assert.Contains("ComponentA", ex.Message);
        Assert.Contains("ComponentB", ex.Message);
        Assert.DoesNotContain("ComponentC", ex.Message);
    }

    [Fact]
    public void ValidateAllComponentsWereInvoked_IncludesTroubleshootingHints()
    {
        TrackingComponentRegistry.Register(new StubTrackingComponent("Test", wasInvoked: false));

        var ex = Assert.Throws<InvalidOperationException>(TrackingComponentRegistry.ValidateAllComponentsWereInvoked);
        Assert.Contains("EF Core", ex.Message);
        Assert.Contains("HTTP", ex.Message);
        Assert.Contains("Redis", ex.Message);
    }

    [Fact]
    public void Clear_RemovesAllComponents()
    {
        TrackingComponentRegistry.Register(new StubTrackingComponent("A", wasInvoked: false));
        TrackingComponentRegistry.Register(new StubTrackingComponent("B", wasInvoked: false));

        TrackingComponentRegistry.Clear();

        Assert.Empty(TrackingComponentRegistry.GetRegisteredComponents());
    }

    private class StubTrackingComponent(string name, bool wasInvoked) : ITrackingComponent
    {
        public string ComponentName => name;
        public bool WasInvoked => wasInvoked;
        public int InvocationCount => wasInvoked ? 1 : 0;
    }
}
