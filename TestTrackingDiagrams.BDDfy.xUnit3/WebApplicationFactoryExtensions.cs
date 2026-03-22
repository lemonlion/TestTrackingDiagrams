using Microsoft.AspNetCore.Mvc.Testing;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public static class WebApplicationFactoryExtensions
{
    public static HttpClient CreateTestTrackingClient<T>(this WebApplicationFactory<T> factory, BDDfyTestTrackingMessageHandlerOptions options) where T : class
    {
        return factory.CreateDefaultClient(new TestTrackingMessageHandler(options));
    }
}
