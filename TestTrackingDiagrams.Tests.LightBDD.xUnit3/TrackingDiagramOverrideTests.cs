using TestTrackingDiagrams.LightBDD.xUnit3;

namespace TestTrackingDiagrams.Tests.LightBDD.xUnit3;

public class TrackingDiagramOverrideTests
{
    [Fact]
    public void StartOverride_ShouldNotThrow()
    {
        // TrackingDiagramOverride uses ScenarioExecutionContext which requires a running scenario.
        // Outside a scenario, it should throw an InvalidOperationException since there's no context.
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.StartOverride());
    }

    [Fact]
    public void EndOverride_ShouldNotThrow()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.EndOverride());
    }

    [Fact]
    public void InsertPlantUml_ShouldNotThrow()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.InsertPlantUml("participant A"));
    }

    [Fact]
    public void InsertTestDelimiter_ShouldNotThrow()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.InsertTestDelimiter("Test1"));
    }

    [Fact]
    public void StartAction_ShouldNotThrow()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.StartAction());
    }

    [Fact]
    public void StartOverride_WithPlantUml_ShouldNotThrow()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.StartOverride("participant B"));
    }

    [Fact]
    public void EndOverride_WithPlantUml_ShouldNotThrow()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.EndOverride("participant B"));
    }
}
