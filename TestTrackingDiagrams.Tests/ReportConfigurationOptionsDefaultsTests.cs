namespace TestTrackingDiagrams.Tests;

public class ReportConfigurationOptionsDefaultsTests
{
    [Fact]
    public void InternalFlowTracking_defaults_to_true()
    {
        var options = new ReportConfigurationOptions();
        Assert.True(options.InternalFlowTracking);
    }
}
