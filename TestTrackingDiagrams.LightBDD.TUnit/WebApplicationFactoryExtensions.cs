using Microsoft.AspNetCore.Mvc.Testing;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD.TUnit;

public static class WebApplicationFactoryExtensions
{
    public static HttpClient CreateTestTrackingClient<T>(this WebApplicationFactory<T> factory, LightBddTestTrackingMessageHandlerOptions options) where T : class
    {
        return factory.CreateDefaultClient(new TestTrackingMessageHandler(options));
    }

    public static HttpClient CreateTestTrackingClient<T>(this WebApplicationFactory<T> factory, LightBddTestTrackingMessageHandlerOptions options, params DelegatingHandler[] additionalHandlers) where T : class
    {
        var handlers = new DelegatingHandler[additionalHandlers.Length + 1];
        handlers[0] = new TestTrackingMessageHandler(options);
        additionalHandlers.CopyTo(handlers, 1);
        return factory.CreateDefaultClient(handlers);
    }
}
