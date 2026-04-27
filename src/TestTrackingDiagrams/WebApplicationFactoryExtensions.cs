using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;

/// <summary>
/// Extension methods for creating test HTTP clients that record requests for diagram generation.
/// </summary>
public static class WebApplicationFactoryExtensions
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> that records all outgoing requests and responses
    /// for use in generated sequence diagrams.
    /// </summary>
    /// <typeparam name="T">The entry point class of the application under test.</typeparam>
    /// <param name="factory">The web application factory.</param>
    /// <param name="options">Options controlling service naming and test context resolution.</param>
    /// <returns>An HTTP client with test-tracking middleware installed.</returns>
    public static HttpClient CreateTestTrackingClient<T>(this WebApplicationFactory<T> factory, TestTrackingMessageHandlerOptions options) where T : class
    {
        options.HttpContextAccessor ??= factory.Services.GetService<IHttpContextAccessor>();
        return factory.CreateDefaultClient(new TestTrackingMessageHandler(options));
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> that records all outgoing requests and responses,
    /// with additional delegating handlers in the pipeline.
    /// </summary>
    /// <typeparam name="T">The entry point class of the application under test.</typeparam>
    /// <param name="factory">The web application factory.</param>
    /// <param name="options">Options controlling service naming and test context resolution.</param>
    /// <param name="additionalHandlers">Additional delegating handlers to include in the HTTP pipeline after the tracking handler.</param>
    /// <returns>An HTTP client with test-tracking middleware and additional handlers installed.</returns>
    public static HttpClient CreateTestTrackingClient<T>(this WebApplicationFactory<T> factory, TestTrackingMessageHandlerOptions options, params DelegatingHandler[] additionalHandlers) where T : class
    {
        options.HttpContextAccessor ??= factory.Services.GetService<IHttpContextAccessor>();
        var handlers = new DelegatingHandler[additionalHandlers.Length + 1];
        handlers[0] = new TestTrackingMessageHandler(options);
        additionalHandlers.CopyTo(handlers, 1);
        return factory.CreateDefaultClient(handlers);
    }
}
