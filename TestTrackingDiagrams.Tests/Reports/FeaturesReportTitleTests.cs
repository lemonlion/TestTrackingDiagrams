using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class FeaturesReportTitleTests
{
    [Fact]
    public void GetFeaturesReportTitle_WithComponentDiagramTitle_UsesTitleAsPrefix()
    {
        var options = new ReportConfigurationOptions
        {
            ComponentDiagramOptions = new ComponentDiagramOptions { Title = "Payment Gateway" }
        };

        Assert.Equal("Payment Gateway - Features Report", ReportGenerator.GetFeaturesReportTitle(options));
    }

    [Fact]
    public void GetFeaturesReportTitle_WithFixedNameForReceivingService_UsesServiceNameAsPrefix()
    {
        var options = new ReportConfigurationOptions
        {
            FixedNameForReceivingService = "OrderService"
        };

        Assert.Equal("OrderService - Features Report", ReportGenerator.GetFeaturesReportTitle(options));
    }

    [Fact]
    public void GetFeaturesReportTitle_WithBothSet_PrefersComponentDiagramTitle()
    {
        var options = new ReportConfigurationOptions
        {
            ComponentDiagramOptions = new ComponentDiagramOptions { Title = "Payment Gateway" },
            FixedNameForReceivingService = "OrderService"
        };

        Assert.Equal("Payment Gateway - Features Report", ReportGenerator.GetFeaturesReportTitle(options));
    }

    [Fact]
    public void GetFeaturesReportTitle_WithEmptyComponentDiagramTitle_FallsBackToServiceName()
    {
        var options = new ReportConfigurationOptions
        {
            ComponentDiagramOptions = new ComponentDiagramOptions { Title = "" },
            FixedNameForReceivingService = "OrderService"
        };

        Assert.Equal("OrderService - Features Report", ReportGenerator.GetFeaturesReportTitle(options));
    }

    [Fact]
    public void GetFeaturesReportTitle_WithNeitherSet_ReturnsFeaturesReport()
    {
        var options = new ReportConfigurationOptions();

        Assert.Equal("Features Report", ReportGenerator.GetFeaturesReportTitle(options));
    }

    [Fact]
    public void GetFeaturesReportTitle_WithNullComponentDiagramOptions_FallsBackToServiceName()
    {
        var options = new ReportConfigurationOptions
        {
            ComponentDiagramOptions = null,
            FixedNameForReceivingService = "MyService"
        };

        Assert.Equal("MyService - Features Report", ReportGenerator.GetFeaturesReportTitle(options));
    }

    [Fact]
    public void GetFeaturesReportTitle_WithEmptyServiceName_ReturnsFeaturesReport()
    {
        var options = new ReportConfigurationOptions
        {
            FixedNameForReceivingService = ""
        };

        Assert.Equal("Features Report", ReportGenerator.GetFeaturesReportTitle(options));
    }
}
