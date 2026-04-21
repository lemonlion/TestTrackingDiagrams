using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests;

[Collection("TrackingComponentRegistry")]
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
    public void GetRegisteredComponents_ReturnsAll()
    {
        TrackingComponentRegistry.Register(new StubTrackingComponent("A", wasInvoked: false));
        TrackingComponentRegistry.Register(new StubTrackingComponent("B", wasInvoked: true));

        Assert.Equal(2, TrackingComponentRegistry.GetRegisteredComponents().Count);
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
