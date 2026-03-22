using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.HttpFakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams;
using TestTrackingDiagrams.BDDfy.xUnit3;
using Xunit;
using CowServiceHttpFake = Example.Api.HttpFakes.CowService.Program;

[assembly: AssemblyFixture(typeof(Example.Api.Tests.Component.BDDfy.xUnit3.Infrastructure.BDDfyTestSetup))]

namespace Example.Api.Tests.Component.BDDfy.xUnit3.Infrastructure;

public class BDDfyTestSetup : IAsyncLifetime
{
    private const string ServiceUnderTestName = "Dessert Provider";

    private static WebApplicationFactory<Program>? _factory;
    private static WebApplicationFactory<CowServiceHttpFake>? _cowServiceHttpFake;
    private static ComponentTestSettings _settings = null!;

    public static WebApplicationFactory<Program> Factory => _factory!;
    public static ComponentTestSettings Settings => _settings;

    public ValueTask InitializeAsync()
    {
        BDDfyDiagramsConfigurator.Configure();
        BDDfyScenarioCollector.StartRunTime = DateTime.UtcNow;

        _settings = new ConfigurationBuilder().GetComponentTestSettings();

        StartHttpFakes();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.TrackDependenciesForDiagrams(new BDDfyTestTrackingMessageHandlerOptions
                {
                    CallingServiceName = ServiceUnderTestName,
                    PortsToServiceNames = { { new Uri(_settings.CowServiceBaseUrl!).Port, "Cow Service" } }
                });
            });
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        BDDfyScenarioCollector.EndRunTime = DateTime.UtcNow;

        BDDfyReportGenerator.CreateStandardReportsWithDiagrams(new ReportConfigurationOptions
        {
            SpecificationsTitle = "Dessert Provider Specifications"
        });

        DisposeHttpFakes();
        _factory?.Dispose();

        return ValueTask.CompletedTask;
    }

    public static HttpClient CreateTrackingClient()
    {
        return _factory!.CreateTestTrackingClient(
            new BDDfyTestTrackingMessageHandlerOptions { FixedNameForReceivingService = ServiceUnderTestName });
    }

    private static void StartHttpFakes()
    {
        DisposeHttpFakes();
        _cowServiceHttpFake = InMemoryFakeHelper.Create<CowServiceHttpFake>(_settings.CowServiceBaseUrl!);
    }

    private static void DisposeHttpFakes()
    {
        try
        {
            _cowServiceHttpFake?.Dispose();
        }
        catch { /* ignored */ }
    }
}
