namespace TestTrackingDiagrams.Tests;

public class ReportConfigurationOptionsDefaultsTests
{
    [Fact]
    public void InternalFlowTracking_defaults_to_true()
    {
        var options = new ReportConfigurationOptions();
        Assert.True(options.InternalFlowTracking);
    }

    [Fact]
    public void WholeTestFlowVisualization_defaults_to_Both()
    {
        var options = new ReportConfigurationOptions();
        Assert.Equal(WholeTestFlowVisualization.Both, options.WholeTestFlowVisualization);
    }

    [Fact]
    public void GenerateComponentDiagram_defaults_to_true()
    {
        var options = new ReportConfigurationOptions();
        Assert.True(options.GenerateComponentDiagram);
    }
}
