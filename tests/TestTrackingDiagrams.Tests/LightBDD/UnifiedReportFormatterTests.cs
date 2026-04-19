using TestTrackingDiagrams.LightBDD;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.LightBDD;

public class UnifiedReportFormatterTests
{
    [Fact]
    public void Default_GroupParameterizedTests_matches_GenerateHtmlReport_default()
    {
        var formatter = new UnifiedReportFormatter();
        Assert.True(formatter.GroupParameterizedTests);
    }

    [Fact]
    public void Default_MaxParameterColumns_matches_GenerateHtmlReport_default()
    {
        var formatter = new UnifiedReportFormatter();
        Assert.Equal(10, formatter.MaxParameterColumns);
    }

    [Fact]
    public void Default_TitleizeParameterNames_matches_GenerateHtmlReport_default()
    {
        var formatter = new UnifiedReportFormatter();
        Assert.True(formatter.TitleizeParameterNames);
    }

    [Fact]
    public void Default_Title_is_TestRunReport()
    {
        var formatter = new UnifiedReportFormatter();
        Assert.Equal("Test Run Report", formatter.Title);
    }
}
