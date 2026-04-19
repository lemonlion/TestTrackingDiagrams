using TestTrackingDiagrams.LightBDD;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.LightBDD;

public class StandardPipelineFormatterTests
{
    [Fact]
    public void Default_Options_is_new_ReportConfigurationOptions()
    {
        var formatter = new StandardPipelineFormatter();
        Assert.NotNull(formatter.Options);
    }

    [Fact]
    public void Default_ExpectedTestCount_is_null()
    {
        var formatter = new StandardPipelineFormatter();
        Assert.Null(formatter.ExpectedTestCount);
    }
}
