using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class TestRunReportTitleTests
{
    [Fact]
    public void GetTestRunReportTitle_WithComponentDiagramTitle_UsesTitleAsPrefix()
    {
        var options = new ReportConfigurationOptions
        {
            ComponentDiagramOptions = new ComponentDiagramOptions { Title = "Payment Gateway" }
        };

        Assert.Equal("Payment Gateway - Test Run Report", ReportGenerator.GetTestRunReportTitle(options));
    }

    [Fact]
    public void GetTestRunReportTitle_WithFixedNameForReceivingService_UsesServiceNameAsPrefix()
    {
        var options = new ReportConfigurationOptions
        {
            FixedNameForReceivingService = "OrderService"
        };

        Assert.Equal("OrderService - Test Run Report", ReportGenerator.GetTestRunReportTitle(options));
    }

    [Fact]
    public void GetTestRunReportTitle_WithBothSet_PrefersComponentDiagramTitle()
    {
        var options = new ReportConfigurationOptions
        {
            ComponentDiagramOptions = new ComponentDiagramOptions { Title = "Payment Gateway" },
            FixedNameForReceivingService = "OrderService"
        };

        Assert.Equal("Payment Gateway - Test Run Report", ReportGenerator.GetTestRunReportTitle(options));
    }

    [Fact]
    public void GetTestRunReportTitle_WithEmptyComponentDiagramTitle_FallsBackToServiceName()
    {
        var options = new ReportConfigurationOptions
        {
            ComponentDiagramOptions = new ComponentDiagramOptions { Title = "" },
            FixedNameForReceivingService = "OrderService"
        };

        Assert.Equal("OrderService - Test Run Report", ReportGenerator.GetTestRunReportTitle(options));
    }

    [Fact]
    public void GetTestRunReportTitle_WithNeitherSet_ReturnsTestRunReport()
    {
        var options = new ReportConfigurationOptions();

        Assert.Equal("Test Run Report", ReportGenerator.GetTestRunReportTitle(options));
    }

    [Fact]
    public void GetTestRunReportTitle_WithNullComponentDiagramOptions_FallsBackToServiceName()
    {
        var options = new ReportConfigurationOptions
        {
            ComponentDiagramOptions = null,
            FixedNameForReceivingService = "MyService"
        };

        Assert.Equal("MyService - Test Run Report", ReportGenerator.GetTestRunReportTitle(options));
    }

    [Fact]
    public void GetTestRunReportTitle_WithEmptyServiceName_ReturnsTestRunReport()
    {
        var options = new ReportConfigurationOptions
        {
            FixedNameForReceivingService = ""
        };

        Assert.Equal("Test Run Report", ReportGenerator.GetTestRunReportTitle(options));
    }
}
