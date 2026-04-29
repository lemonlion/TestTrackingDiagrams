using Example.Api.Events;
using Example.Api.Tests.Component.Shared;
using Example.Api.Tests.Component.Shared.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;
using TestTrackingDiagrams.NUnit4;

namespace Example.Api.Tests.Component.NUnit4.Infrastructure;

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
                    CallerName = ServiceUnderTestName,
                    PortsToServiceNames = { { new Uri(Settings.CowServiceBaseUrl!).Port, "Cow Service" } }
                });
                services.TrackMessagesForDiagrams(ServiceUnderTestName);
                services.AddSingleton<IEventPublisher, FakeEventPublisher>();
            });
        });
    }

    protected BaseFixture()
    {
        Client = SFactory!.CreateTestTrackingClient(new NUnitTestTrackingMessageHandlerOptions { FixedNameForReceivingService = ServiceUnderTestName });
    }

    public void Dispose() => Client.Dispose();
}

