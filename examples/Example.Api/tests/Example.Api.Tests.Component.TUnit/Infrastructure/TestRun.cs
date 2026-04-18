using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.HttpFakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams;
using TestTrackingDiagrams.TUnit;
using TUnit.Core;
using CowServiceHttpFake = Example.Api.HttpFakes.CowService.Program;

namespace Example.Api.Tests.Component.TUnit.Infrastructure;

public class TestRun : DiagrammedTestRun
{
    private static WebApplicationFactory<CowServiceHttpFake>? _cowServiceHttpFake;

    [Before(Assembly)]
    public static void GlobalSetup(AssemblyHookContext context)
    {
        Setup();
        StartHttpFakes();
    }

    [After(Assembly)]
    public static void GlobalTeardown(AssemblyHookContext context)
    {
        EndRunTime = DateTime.UtcNow;
        DisposeHttpFakes();

        // When run by the integration test project, configuration is provided via environment variables.
        // Otherwise, the hardcoded values below serve as a readable example for users.
        var reportOptions = IntegrationTestConfiguration.IsIntegrationTestMode
            ? IntegrationTestConfiguration.GetReportConfigurationOptions()
            : new ReportConfigurationOptions
            {
                SpecificationsTitle = "Dessert Provider Specifications",
                SeparateSetup = true,
            };

        TUnitReportGenerator.CreateStandardReportsWithDiagrams(TestContexts, StartRunTime, EndRunTime, reportOptions);
    }

    private static void StartHttpFakes()
    {
        DisposeHttpFakes();
        _cowServiceHttpFake = WebApplicationFactoryForSpecificUrl<CowServiceHttpFake>.Create(Settings.CowServiceBaseUrl!);
    }

    private static void DisposeHttpFakes()
    {
        try
        {
            _cowServiceHttpFake?.Dispose();
        }
        catch { /* ignored */ }
    }

    private static ComponentTestSettings Settings { get; } = new ConfigurationBuilder().GetComponentTestSettings();
}
