using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;

/// <summary>
/// An <see cref="IHttpClientFactory"/> implementation that creates <see cref="HttpClient"/> instances
/// pre-configured with <see cref="TestTrackingMessageHandler"/> for automatic HTTP tracking in diagrams.
/// </summary>
public class TestTrackingHttpClientFactory(
    IHttpContextAccessor httpContextAccessor,
    TestTrackingMessageHandlerOptions testTrackingMessageHandlerOptions)
    : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(new TestTrackingMessageHandler(testTrackingMessageHandlerOptions, httpContextAccessor));
}