using System.Reflection;
using Example.Api.Tests.Component.LightBDD.xUnit2.Infrastructure;
using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.HttpFakes;
using LightBDD.Contrib.ProgressNotifierEnhancements;
using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;
using LightBDD.XUnit2;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams;
using TestTrackingDiagrams.LightBDD.xUnit2;
using CowServiceHttpFake = Example.Api.HttpFakes.CowService.Program;

[assembly: ConfiguredLightBddScope]
[assembly: ClassCollectionBehavior(AllowTestParallelization = false)]
namespace Example.Api.Tests.Component.LightBDD.xUnit2.Infrastructure;

internal class ConfiguredLightBddScopeAttribute : LightBddScopeAttribute
{
    private static WebApplicationFactory<CowServiceHttpFake>? _cowServiceHttpFake;

    protected override void OnConfigure(LightBddConfiguration configuration)
    {
        var testAssembly = Assembly.GetAssembly(typeof(ConfiguredLightBddScopeAttribute))!;

        configuration.ReportWritersConfiguration().CreateStandardReportsWithDiagrams(testAssembly,
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "Dessert Provider Specifications",
                SeparateSetup = true,
            });

        // To stop the output repeating the step name for each step
        configuration.ProgressNotifierConfiguration().Clear().Append(new ConfigurableProgressNotifier());

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