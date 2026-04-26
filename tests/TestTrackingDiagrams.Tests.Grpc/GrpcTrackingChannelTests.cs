using Grpc.Core;
using TestTrackingDiagrams.Extensions.Grpc;

namespace TestTrackingDiagrams.Tests.Grpc;

public class GrpcTrackingChannelTests
{
    [Fact]
    public void Create_returns_non_null_CallInvoker()
    {
        var handler = new TestHttpMessageHandler();
        var options = new GrpcTrackingOptions
        {
            ServiceName = "My API",
            CallingServiceName = "Test"
        };

        var invoker = GrpcTrackingChannel.Create(handler, new Uri("http://localhost"), options);

        Assert.NotNull(invoker);
    }

    [Fact]
    public void Create_with_base_address_returns_non_null_CallInvoker()
    {
        var handler = new TestHttpMessageHandler();
        var options = new GrpcTrackingOptions
        {
            ServiceName = "My API",
            CallingServiceName = "Test"
        };

        var (invoker, channel) = GrpcTrackingChannel.CreateWithChannel(
            handler, new Uri("http://localhost"), options);

        Assert.NotNull(invoker);
        Assert.NotNull(channel);
        channel.Dispose();
    }

    [Fact]
    public void HttpMessageHandler_extension_returns_non_null_CallInvoker()
    {
        var handler = new TestHttpMessageHandler();
        var options = new GrpcTrackingOptions
        {
            ServiceName = "My API",
            CallingServiceName = "Test"
        };

        var invoker = handler.AsGrpcTrackingCallInvoker(new Uri("http://localhost"), options);

        Assert.NotNull(invoker);
    }

    /// <summary>Minimal handler that always returns 200 (enough for channel creation).</summary>
    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
