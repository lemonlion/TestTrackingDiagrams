using TestTrackingDiagrams.LightBDD.TUnit;

namespace TestTrackingDiagrams.Tests.LightBDD.TUnit;

public class TrackingDiagramOverrideTests
{
    [Fact]
    public void StartOverride_ShouldThrowOutsideScenarioContext()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.StartOverride());
    }

    [Fact]
    public void EndOverride_ShouldThrowOutsideScenarioContext()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.EndOverride());
    }

    [Fact]
    public void InsertPlantUml_ShouldThrowOutsideScenarioContext()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.InsertPlantUml("participant A"));
    }

    [Fact]
    public void InsertTestDelimiter_ShouldThrowOutsideScenarioContext()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.InsertTestDelimiter("Test1"));
    }

    [Fact]
    public void StartAction_ShouldThrowOutsideScenarioContext()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.StartAction());
    }

    [Fact]
    public void StartOverride_WithPlantUml_ShouldThrowOutsideScenarioContext()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.StartOverride("participant B"));
    }

    [Fact]
    public void EndOverride_WithPlantUml_ShouldThrowOutsideScenarioContext()
    {
        Assert.ThrowsAny<Exception>(() => TrackingDiagramOverride.EndOverride("participant B"));
    }
}
