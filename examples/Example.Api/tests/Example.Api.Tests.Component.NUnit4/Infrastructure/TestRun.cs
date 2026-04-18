using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.HttpFakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams;
using TestTrackingDiagrams.NUnit4;
using CowServiceHttpFake = Example.Api.HttpFakes.CowService.Program;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)] 

#pragma warning disable CA1050
// ReSharper disable once CheckNamespace
// We specifically run this outside of a namespace so that the `OneTimeSetUp` and `OneTimeTearDown` run for the entire test run of any test in the entire assembly

[SetUpFixture]
public class TestRun : DiagrammedTestRun
{
    private static WebApplicationFactory<CowServiceHttpFake>? _cowServiceHttpFake;

    [OneTimeSetUp]
    public static void GlobalSetup()
    {
        Setup();
        StartHttpFakes();
    }

    [OneTimeTearDown]
    public static void GlobalTeardown()
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

        NUnitReportGenerator.CreateStandardReportsWithDiagrams(TestContexts, StartRunTime, EndRunTime, reportOptions);

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