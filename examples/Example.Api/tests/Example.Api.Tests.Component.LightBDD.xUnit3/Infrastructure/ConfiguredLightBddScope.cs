using Example.Api.Tests.Component.LightBDD.xUnit3.Infrastructure;
using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.HttpFakes;
using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;
using LightBDD.XUnit3;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams;
using TestTrackingDiagrams.LightBDD.xUnit3;
using Xunit.v3;
using CowServiceHttpFake = Example.Api.HttpFakes.CowService.Program;

[assembly: TestPipelineStartup(typeof(ConfiguredLightBddScope))]
[assembly: CaptureLightBddArguments]
namespace Example.Api.Tests.Component.LightBDD.xUnit3.Infrastructure;

public class ConfiguredLightBddScope : LightBddScope
{
    private static WebApplicationFactory<CowServiceHttpFake>? _cowServiceHttpFake;

    protected override void OnConfigure(LightBddConfiguration configuration)
    {
        // When run by the integration test project, configuration is provided via environment variables.
        // Otherwise, the hardcoded values below serve as a readable example for users.
        var reportOptions = IntegrationTestConfiguration.IsIntegrationTestMode
            ? IntegrationTestConfiguration.GetReportConfigurationOptions()
            : new ReportConfigurationOptions
            {
                SpecificationsTitle = "Dessert Provider Specifications",
                SeparateSetup = true,
            };

        configuration.ReportWritersConfiguration().CreateStandardReportsWithDiagrams(reportOptions);

        // To stop the output repeating the step name for each step
        configuration.ProgressNotifierConfiguration().Clear();

        configuration.ExecutionExtensionsConfiguration()
                .RegisterGlobalTearDown("dispose factory", BaseFixture.DisposeFactory)
                .RegisterGlobalSetUp("http fakes", StartHttpFakes, DisposeHttpFakes);
    }
    
    private void StartHttpFakes()
    {
        DisposeHttpFakes();

        _cowServiceHttpFake = WebApplicationFactoryForSpecificUrl<CowServiceHttpFake>.Create(Settings.CowServiceBaseUrl!);
    }

    private void DisposeHttpFakes()
    {
        try
        {
            _cowServiceHttpFake?.Dispose();
        }
        catch { /* ignored */ }
    }

    private ComponentTestSettings Settings { get; } = new ConfigurationBuilder().GetComponentTestSettings();
}
