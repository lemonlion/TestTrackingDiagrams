using LightBDD.Core.Configuration;
using LightBDD.Core.Extensibility.Execution;
using LightBDD.Framework.Configuration;
using TestTrackingDiagrams.LightBDD;
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

    [Fact]
    public void CreateStandardReportsWithDiagrams_LightBddConfiguration_registers_step_tracking_step_decorator()
    {
        var configuration = new LightBddConfiguration();
        var options = new ReportConfigurationOptions();

        configuration.CreateStandardReportsWithDiagrams(options);

        var stepDecorators = configuration.ExecutionExtensionsConfiguration().StepDecorators;
        Assert.Contains(stepDecorators, d => d is StepTrackingStepDecorator);
    }
}
