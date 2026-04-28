using System.Net;
using TestTrackingDiagrams.Extensions.Grpc;

namespace TestTrackingDiagrams.Tests.Grpc;

public class GrpcResponseVersionHandlerTests
{
    [Fact]
    public async Task SendAsync_copies_request_version_to_response()
    {
        var innerHandler = new FakeInnerHandler(new Version(1, 1));
        var handler = new GrpcResponseVersionHandler(innerHandler);
        var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/test")
        {
            Version = new Version(2, 0)
        };

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(new Version(2, 0), response.Version);
    }

    [Fact]
    public async Task SendAsync_preserves_response_content_and_status()
    {
        var innerHandler = new FakeInnerHandler(new Version(1, 1), HttpStatusCode.NotFound);
        var handler = new GrpcResponseVersionHandler(innerHandler);
        var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test")
        {
            Version = new Version(2, 0)
        };

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_when_versions_match_response_unchanged()
    {
        var innerHandler = new FakeInnerHandler(new Version(2, 0));
        var handler = new GrpcResponseVersionHandler(innerHandler);
        var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/test")
        {
            Version = new Version(2, 0)
        };

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(new Version(2, 0), response.Version);
    }

    private sealed class FakeInnerHandler(Version responseVersion, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Version = responseVersion
            });
        }
    }
}
