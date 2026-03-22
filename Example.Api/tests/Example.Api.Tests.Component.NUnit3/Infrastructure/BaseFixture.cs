using Example.Api.Tests.Component.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams.NUnit3;

namespace Example.Api.Tests.Component.NUnit3.Infrastructure;

public abstract class BaseFixture : DiagrammedComponentTest, IDisposable
{
    private static readonly WebApplicationFactory<Program>? SFactory;
    protected static ComponentTestSettings Settings { get; }
    protected HttpClient Client { get; }

    private const string ServiceUnderTestName = "Dessert Provider";

    static BaseFixture()
    {
        Settings = new ConfigurationBuilder().GetComponentTestConfiguration().Get<ComponentTestSettings>()!;

        SFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("CowServiceBaseUrl", Settings.CowServiceBaseUrl);
            builder.ConfigureTestServices(services =>
            {
                services.TrackDependenciesForDiagrams(new NUnitTestTrackingMessageHandlerOptions
                {
                    CallingServiceName = ServiceUnderTestName,
                    PortsToServiceNames = { { new Uri(Settings.CowServiceBaseUrl!).Port, "Cow Service" } }
                });
            });
        });
    }

    protected BaseFixture()
    {
        Client = SFactory!.CreateTestTrackingClient(new NUnitTestTrackingMessageHandlerOptions { FixedNameForReceivingService = ServiceUnderTestName });
    }

    public void Dispose() => Client.Dispose();
}

