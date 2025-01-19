using Example.Api.Tests.Component.Shared;
using LightBDD.XUnit2;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using TestTrackingDiagrams.LightBDD.XUnit;

namespace Example.Api.Tests.Component.LightBDD.XUnit.Infrastructure;

public abstract class BaseFixture : FeatureFixture, IDisposable
{
    private static readonly WebApplicationFactory<Program>? SFactory;
    public static ComponentTestSettings Settings { get; }
    protected HttpClient Client { get; }

    private const string ServiceUnderTestName = "Dessert Provider";

    static BaseFixture()
    {
        SFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var settings = new ConfigurationBuilder().GetComponentTestConfiguration().Get<ComponentTestSettings>()!;
                services.TrackDependenciesForDiagrams(new LightBddTestTrackingMessageHandlerOptions
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
        Client = SFactory!.CreateTestTrackingClient(new LightBddTestTrackingMessageHandlerOptions { FixedNameForReceivingService = ServiceUnderTestName });
    }

    public void Dispose() => Client.Dispose();
    public static void DisposeFactory() => SFactory?.Dispose();
}