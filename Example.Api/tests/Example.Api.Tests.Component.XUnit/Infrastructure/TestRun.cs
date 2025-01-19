using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.HttpFakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams;
using TestTrackingDiagrams.XUnit;
using CowServiceHttpFake = Example.Api.HttpFakes.CowService.Program;

namespace Example.Api.Tests.Component.XUnit.Infrastructure;

public class TestRun : DiagrammedTestRun, IDisposable
{
    private static WebApplicationFactory<CowServiceHttpFake>? _cowServiceHttpFake;

    public TestRun()
    {
        StartHttpFakes();
    }

    public void Dispose()
    {
        EndRunTime = DateTime.UtcNow;
        DisposeHttpFakes();

        XUnitReportGenerator.CreateStandardReportsWithDiagrams(TestContexts, StartRunTime, EndRunTime,
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "Dessert Provider Specifications",
            });

    }

    private void StartHttpFakes()
    {
        DisposeHttpFakes();
        _cowServiceHttpFake = InMemoryFakeHelper.Create<CowServiceHttpFake>(Settings.CowServiceBaseUrl!);
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