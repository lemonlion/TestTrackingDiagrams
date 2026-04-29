using Example.Api.Events;
using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.Fakes;
using Example.Api.Tests.Component.Shared.HttpFakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams;
using TestTrackingDiagrams.BDDfy.xUnit3;
using TestTrackingDiagrams.Tracking;
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
            builder.UseSetting("CowServiceBaseUrl", _settings.CowServiceBaseUrl);
            builder.ConfigureTestServices(services =>
            {
                services.TrackDependenciesForDiagrams(new BDDfyTestTrackingMessageHandlerOptions
                {
                    CallerName = ServiceUnderTestName,
                    PortsToServiceNames = { { new Uri(_settings.CowServiceBaseUrl!).Port, "Cow Service" } }
                });
                services.TrackMessagesForDiagrams(ServiceUnderTestName);
                services.AddSingleton<IEventPublisher, FakeEventPublisher>();
            });
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        BDDfyScenarioCollector.EndRunTime = DateTime.UtcNow;

        // When run by the integration test project, configuration is provided via environment variables.
        // Otherwise, the hardcoded values below serve as a readable example for users.
        var reportOptions = IntegrationTestConfiguration.IsIntegrationTestMode
            ? IntegrationTestConfiguration.GetReportConfigurationOptions()
            : new ReportConfigurationOptions
            {
                SpecificationsTitle = "Dessert Provider Specifications",
                SeparateSetup = true,
            };

        BDDfyReportGenerator.CreateStandardReportsWithDiagrams(reportOptions);

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
        _cowServiceHttpFake = WebApplicationFactoryForSpecificUrl<CowServiceHttpFake>.Create(_settings.CowServiceBaseUrl!);
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
