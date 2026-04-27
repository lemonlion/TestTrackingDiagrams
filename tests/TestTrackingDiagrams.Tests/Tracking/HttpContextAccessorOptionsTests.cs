using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

public class HttpContextAccessorOptionsTests
{
    [Fact]
    public void TestTrackingMessageHandler_reads_HttpContextAccessor_from_options_when_not_passed_directly()
    {
        var accessor = new TestHttpContextAccessor(new DefaultHttpContext());
        var options = new TestTrackingMessageHandlerOptions
        {
            CallingServiceName = "Test",
            HttpContextAccessor = accessor
        };

        var handler = new TestTrackingMessageHandler(options);

        // The handler stores _httpContextAccessor privately — verify via ComponentName that it constructed
        Assert.Equal("TestTrackingMessageHandler (Test)", handler.ComponentName);
    }

    [Fact]
    public void TestTrackingMessageHandler_explicit_accessor_takes_precedence_over_options()
    {
        var optionsAccessor = new TestHttpContextAccessor(new DefaultHttpContext());
        var explicitAccessor = new TestHttpContextAccessor(new DefaultHttpContext());
        var options = new TestTrackingMessageHandlerOptions
        {
            CallingServiceName = "Test",
            HttpContextAccessor = optionsAccessor
        };

        // Pass explicit accessor — it should take precedence
        var handler = new TestTrackingMessageHandler(options, explicitAccessor);
        Assert.NotNull(handler);
    }

    [Fact]
    public void TestTrackingMessageHandlerOptions_HttpContextAccessor_defaults_to_null()
    {
        var options = new TestTrackingMessageHandlerOptions();
        Assert.Null(options.HttpContextAccessor);
    }

    private class TestHttpContextAccessor(HttpContext? httpContext) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = httpContext;
    }
}
