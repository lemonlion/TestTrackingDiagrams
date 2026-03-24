using Microsoft.AspNetCore.Mvc.Testing;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.xUnit3;

public static class WebApplicationFactoryExtensions
{
    public static HttpClient CreateTestTrackingClient<T>(this WebApplicationFactory<T> factory, XUnitTestTrackingMessageHandlerOptions options) where T : class
    {
        return factory.CreateDefaultClient(new TestTrackingMessageHandler(options));
    }
}