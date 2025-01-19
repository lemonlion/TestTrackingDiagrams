using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.HttpFakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams;
using TestTrackingDiagrams.NUnit;
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

        NUnitReportGenerator.CreateStandardReportsWithDiagrams(TestContexts, StartRunTime, EndRunTime,
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "Dessert Provider Specifications",
            });

    }

    private static void StartHttpFakes()
    {
        DisposeHttpFakes(); 

        _cowServiceHttpFake = InMemoryFakeHelper.Create<CowServiceHttpFake>(Settings.CowServiceBaseUrl!);
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