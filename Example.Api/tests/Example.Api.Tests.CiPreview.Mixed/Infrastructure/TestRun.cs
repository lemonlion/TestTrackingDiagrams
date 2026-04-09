using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.HttpFakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams;
using TestTrackingDiagrams.xUnit3;
using CowServiceHttpFake = Example.Api.HttpFakes.CowService.Program;

namespace Example.Api.Tests.CiPreview.Mixed.Infrastructure;

public class TestRun : DiagrammedTestRun, IDisposable
{
    private static WebApplicationFactory<CowServiceHttpFake>? _cowServiceHttpFake;

    public TestRun() => StartHttpFakes();

    public void Dispose()
    {
        EndRunTime = DateTime.UtcNow;
        DisposeHttpFakes();

        XUnitReportGenerator.CreateStandardReportsWithDiagrams(TestContexts, StartRunTime, EndRunTime,
            new ReportConfigurationOptions { SpecificationsTitle = "CI Preview — Mixed Results" });
    }

    private void StartHttpFakes()
    {
        DisposeHttpFakes();
        _cowServiceHttpFake = WebApplicationFactoryForSpecificUrl<CowServiceHttpFake>.Create(
            new ConfigurationBuilder().GetComponentTestConfiguration().Get<ComponentTestSettings>()!.CowServiceBaseUrl!);
    }

    private void DisposeHttpFakes()
    {
        try { _cowServiceHttpFake?.Dispose(); } catch { }
    }
}
