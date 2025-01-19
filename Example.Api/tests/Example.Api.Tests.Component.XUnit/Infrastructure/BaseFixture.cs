using Example.Api.Tests.Component.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams.XUnit;

namespace Example.Api.Tests.Component.XUnit.Infrastructure;

public abstract class BaseFixture : DiagrammedComponentTest
{
    private static readonly WebApplicationFactory<Program>? SFactory;
    protected static ComponentTestSettings Settings { get; }
    protected HttpClient Client { get; }

    private const string ServiceUnderTestName = "Dessert Provider";

    static BaseFixture()
    {
        SFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var settings = new ConfigurationBuilder().GetComponentTestConfiguration().Get<ComponentTestSettings>()!;
                services.TrackDependenciesForDiagrams(new XUnitTestTrackingMessageHandlerOptions
                {
                    CallingServiceName = ServiceUnderTestName,
                    PortsToServiceNames = { { new Uri(settings.CowServiceBaseUrl!).Port, "Cow Service" } }
                });
            });
        });

        Settings = new ConfigurationBuilder().GetComponentTestConfiguration().Get<ComponentTestSettings>()!;
    }

    protected BaseFixture()
    {
        Client = SFactory!.CreateTestTrackingClient(new XUnitTestTrackingMessageHandlerOptions { FixedNameForReceivingService = ServiceUnderTestName });
    }

    public void Dispose(bool disposing) => Client?.Dispose();
}

