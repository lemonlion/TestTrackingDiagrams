using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;

public class TestTrackingHttpClientFactory(
    IHttpContextAccessor httpContextAccessor,
    TestTrackingMessageHandlerOptions testTrackingMessageHandlerOptions)
    : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(new TestTrackingMessageHandler(testTrackingMessageHandlerOptions, httpContextAccessor));
}