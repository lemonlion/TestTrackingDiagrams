using Example.Api.Tests.Component.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams.NUnit;

namespace Example.Api.Tests.Component.NUnit.Infrastructure;

public abstract class BaseFixture : DiagrammedComponentTest, IDisposable
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
                services.TrackDependenciesForDiagrams(new NUnitTestTrackingMessageHandlerOptions
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
        Client = SFactory!.CreateTestTrackingClient(new NUnitTestTrackingMessageHandlerOptions { FixedNameForReceivingService = ServiceUnderTestName });
    }

    public void Dispose() => Client.Dispose();
}

