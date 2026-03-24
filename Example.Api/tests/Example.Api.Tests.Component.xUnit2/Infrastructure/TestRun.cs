using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.HttpFakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams;
using TestTrackingDiagrams.xUnit2;
using CowServiceHttpFake = Example.Api.HttpFakes.CowService.Program;

namespace Example.Api.Tests.Component.xUnit2.Infrastructure;

public class TestRun : DiagrammedTestRun, IDisposable
{
    private static WebApplicationFactory<CowServiceHttpFake>? _cowServiceHttpFake;

    public TestRun()
    {
        // Configure report options for the ReportingTestFramework
        ReportLifecycle.Options = new ReportConfigurationOptions
        {
            SpecificationsTitle = "Dessert Provider Specifications",
            SeparateSetup = true,
        };

        StartHttpFakes();
    }

    public void Dispose()
    {
        EndRunTime = DateTime.UtcNow;
        DisposeHttpFakes();
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
