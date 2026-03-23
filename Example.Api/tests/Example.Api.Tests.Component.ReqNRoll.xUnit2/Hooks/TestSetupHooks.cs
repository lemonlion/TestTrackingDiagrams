using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.HttpFakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using Reqnroll.BoDi;
using TestTrackingDiagrams;
using TestTrackingDiagrams.ReqNRoll.xUnit2;
using CowServiceHttpFake = Example.Api.HttpFakes.CowService.Program;

namespace Example.Api.Tests.Component.ReqNRoll.xUnit2.Hooks;

[Binding]
public class TestSetupHooks
{
    private const string ServiceUnderTestName = "Dessert Provider";

    private static WebApplicationFactory<Program>? _factory;
    private static WebApplicationFactory<CowServiceHttpFake>? _cowServiceHttpFake;
    private static ComponentTestSettings _settings = null!;

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        _settings = new ConfigurationBuilder().GetComponentTestSettings();

        StartHttpFakes();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("CowServiceBaseUrl", _settings.CowServiceBaseUrl);
            builder.ConfigureTestServices(services =>
            {
                services.TrackDependenciesForDiagrams(new ReqNRollTestTrackingMessageHandlerOptions
                {
                    CallingServiceName = ServiceUnderTestName,
                    PortsToServiceNames = { { new Uri(_settings.CowServiceBaseUrl!).Port, "Cow Service" } }
                });
            });
        });
    }

    [BeforeScenario]
    public void BeforeScenario(IObjectContainer objectContainer)
    {
        var client = _factory!.CreateTestTrackingClient(
            new ReqNRollTestTrackingMessageHandlerOptions { FixedNameForReceivingService = ServiceUnderTestName });
        objectContainer.RegisterInstanceAs(client);
    }

    [AfterScenario]
    public void AfterScenario(IObjectContainer objectContainer)
    {
        var client = objectContainer.Resolve<HttpClient>();
        client.Dispose();
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        ReqNRollReportGenerator.CreateStandardReportsWithDiagrams(new ReportConfigurationOptions
        {
            SpecificationsTitle = "Dessert Provider Specifications",
            SeparateSetup = true,
        });

        DisposeHttpFakes();
        _factory?.Dispose();
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
