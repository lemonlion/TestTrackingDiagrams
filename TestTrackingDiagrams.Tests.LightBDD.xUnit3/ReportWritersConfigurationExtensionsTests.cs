using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;
using TestTrackingDiagrams.LightBDD.xUnit3;

namespace TestTrackingDiagrams.Tests.LightBDD.xUnit3;

public class ReportWritersConfigurationExtensionsTests
{
    [Fact]
    public void CreateStandardReportsWithDiagrams_ShouldReturnConfiguration()
    {
        var configuration = new LightBddConfiguration();
        var reportWritersConfig = configuration.ReportWritersConfiguration();
        var options = new ReportConfigurationOptions();

        var result = reportWritersConfig.CreateStandardReportsWithDiagrams(options);

        Assert.NotNull(result);
        Assert.IsType<ReportWritersConfiguration>(result);
    }

    [Fact]
    public void CreateStandardReportsWithDiagrams_ShouldAcceptCustomOptions()
    {
        var configuration = new LightBddConfiguration();
        var reportWritersConfig = configuration.ReportWritersConfiguration();
        var options = new ReportConfigurationOptions
        {
            SpecificationsTitle = "Custom Title",
            PlantUmlServerBaseUrl = "http://custom-server.com"
        };

        var result = reportWritersConfig.CreateStandardReportsWithDiagrams(options);

        Assert.NotNull(result);
    }
}
